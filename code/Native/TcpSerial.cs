using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using RJCP.IO.Ports.Native.Tcp;
using RJCP.IO.Ports.Trace;

namespace RJCP.IO.Ports.Native
{
    internal class TcpSerial : INativeSerial
    {
        public event EventHandler<SerialDataReceivedEventArgs> DataReceived;

        public event EventHandler<SerialErrorReceivedEventArgs> ErrorReceived;

        public event EventHandler<SerialPinChangedEventArgs> PinChanged;

        public int BaudRate { get; set; }

        public int DataBits { get; set; }

        public Parity Parity { get; set; }

        public StopBits StopBits { get; set; }

        public bool DiscardNull { get; set; }

        public byte ParityReplace { get; set; }

        public bool TxContinueOnXOff { get; set; }

        public int XOffLimit { get; set; }

        public int XOnLimit { get; set; }

        public bool BreakState { get; set; }

        public bool DtrEnable { get; set; }

        public bool RtsEnable { get; set; }

        public Handshake Handshake { get; set; }

        public string PortName { get; set; }

        public bool CDHolding => false;

        public bool CtsHolding => false;

        public bool DsrHolding => false;

        public bool RingHolding => false;

        public int BytesToRead => m_Buffer?.Serial.ReadBuffer.Length ?? 0;

        public int BytesToWrite => m_Buffer?.Serial.WriteBuffer.Length ?? 0;

        public bool IsOpen => m_Socket != null && m_Socket.Connected;

        public bool IsRunning => m_Socket != null && m_Socket.Connected;

        public string Version => "N/A";
        
        public int DriverInQueue
        {
            get => -1;
            set
            {
                // ignored
            }
        }

        public int DriverOutQueue
        {
            get => -1;
            set
            {
                // ignored
            }
        }

        private Socket m_Socket;

        private string m_Host;

        private int m_Port;

        private readonly AutoResetEvent m_ReceiveWaiter;

        private SerialBuffer m_Buffer;

        private string m_Name;

        public TcpSerial()
        {
            m_ReceiveWaiter = new AutoResetEvent(false);

            PortName = $"{m_Host}:{m_Port}";
        }

        private Socket CreateSocket()
        {
            return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
        
        public void UpdateSettings(SerialPortSettings settings)
        {
            if (settings is TcpSerialPortSettings tcpSerialPortSettings)
            {
                m_Host = tcpSerialPortSettings.RemoteHost;
                m_Port = tcpSerialPortSettings.RemotePort;
                BaudRate = tcpSerialPortSettings.BaudRate;
                DataBits = tcpSerialPortSettings.DataBits;
                Parity = tcpSerialPortSettings.Parity;
                StopBits = tcpSerialPortSettings.StopBits;
                Handshake = tcpSerialPortSettings.Handshake;

                tcpSerialPortSettings.SerialPortSettingsManager?.SetSettings(tcpSerialPortSettings);
            }
            else
            {
                throw new InvalidOperationException("Expected TcpSerialPortSettings!");
            }
        }

        public string[] GetPortNames()
        {
            return new[] { PortName };
        }

        public PortDescription[] GetPortDescriptions()
        {
            return new[] { new PortDescription(PortName, "N/A") };
        }

        public void Open()
        {
            ConnectSocket();

            Task.Run(ReceiveLoop);
        }

        private void ConnectSocket()
        {
            m_Socket = CreateSocket();
            
            m_Socket.Connect(m_Host, m_Port);
        }

        public void Close()
        {
            m_Socket.Close();

            m_ReceiveWaiter.Set();

            m_Buffer.Serial.Purge();

            WaitWhileCondition(() => IsOpen);
        }

        private void WaitWhileCondition(Func<bool> condition, int timeout = 1000)
        {
            var stopWatch = new Stopwatch();
            
            stopWatch.Start();
            
            while (condition() && stopWatch.ElapsedMilliseconds < timeout)
            {
                Thread.Sleep(1);
            }
            
            stopWatch.Stop();
        }
        
        public SerialBuffer CreateSerialBuffer(int readBuffer, int writeBuffer)
        {
            return new SerialBuffer(readBuffer, writeBuffer, true);
        }

        public void StartMonitor(SerialBuffer buffer, string name)
        {
            if (!IsOpen) throw new InvalidOperationException("Serial Port not open");

            m_Buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            m_Name = name;

            m_Buffer.WriteEvent += BufferOnWriteEvent;
        }

        public void Dispose()
        {
            Close();

            m_Socket?.Dispose();
        }

        public void DiscardInBuffer()
        {
            // do nothing
        }

        public void DiscardOutBuffer()
        {
            // do nothing
        }

        public void GetPortSettings()
        {
            // do nothing
        }

        public void SetPortSettings()
        {
            // do nothing
        }

        private void ReceiveLoop()
        {
            while (IsOpen)
            {
                if (!m_Socket.Connected)
                {
                    ConnectSocket();
                }
                
                m_Socket.BeginReceive(m_Buffer.Serial.ReadBuffer.Array, m_Buffer.Serial.ReadBuffer.End,
                    m_Buffer.Serial.ReadBuffer.WriteLength, SocketFlags.None, ReceiveCallback, null);

                m_ReceiveWaiter.WaitOne();
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                if (!m_Socket.Connected)
                {
                    ConnectSocket();
                }
                
                var receivedBytes = m_Socket.EndReceive(ar);

                if (receivedBytes > 0)
                {
                    m_Buffer.Serial.ReadBufferProduce(receivedBytes);

                    DataReceived?.Invoke(this, new SerialDataReceivedEventArgs(SerialData.Chars));
                }

                m_ReceiveWaiter.Set();
            }
            catch (Exception ex)
            {
                Log.Serial.TraceEvent(TraceEventType.Error, 0, $"{m_Name}: ReceiveCallback: {ex.Message}");
            }
        }

        private void BufferOnWriteEvent(object sender, EventArgs e)
        {
            try
            {
                if (!m_Socket.Connected)
                {
                    ConnectSocket();
                }
            
                var data = new byte[m_Buffer.Serial.WriteBuffer.ReadLength];

                Array.Copy(m_Buffer.Serial.WriteBuffer.Array, m_Buffer.Serial.WriteBuffer.Start, data, 0,
                    m_Buffer.Serial.WriteBuffer.ReadLength);

                m_Socket.Send(data);

                m_Buffer.Serial.WriteBufferConsume(m_Buffer.Serial.WriteBuffer.ReadLength);
                m_Buffer.Serial.TxEmptyEvent();
            }
            catch (Exception ex)
            {
                Log.Serial.TraceEvent(TraceEventType.Error, 0, $"{m_Name}: BufferOnWriteEvent: {ex.Message}");
            }
        }
    }
}
