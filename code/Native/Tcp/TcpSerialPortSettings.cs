namespace RJCP.IO.Ports.Native.Tcp
{
    public class TcpSerialPortSettings : SerialPortSettings
    {
        public string RemoteHost { get; set; }

        public int RemotePort { get; set; }

        public int UartPacketTime { get; set; } = 0;

        public int UartPacketLength { get; set; } = 0;

        public bool SyncBaudRate { get; set; } = true;

        public ITcpSerialPortSettingsManager SerialPortSettingsManager { get; set; }

        public TcpSerialPortSettings()
        {
            BaudRate = 115200;
            DataBits = 8;
            Parity = Parity.None;
            StopBits = StopBits.One;
            Handshake = Handshake.None;
        }

        public TcpSerialPortSettings(SerialPortSettings settings)
        {
            PortName = settings.PortName;
            BaudRate = settings.BaudRate;
            DataBits = settings.DataBits;
            Parity = settings.Parity;
            StopBits = settings.StopBits;
            Handshake = settings.Handshake;
        }
    }
}
