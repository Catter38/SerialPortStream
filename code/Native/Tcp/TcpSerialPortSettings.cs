namespace RJCP.IO.Ports.Native.Tcp
{
    public class TcpSerialPortSettings : SerialPortSettings
    {
        public new string PortName
        {
            get => $"tcp://{RemoteHost}:{RemotePort}";
            set => UpdateHostAndPortFromPortName(value);
        }

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
            UpdateHostAndPortFromPortName(settings.PortName);

            BaudRate = settings.BaudRate;
            DataBits = settings.DataBits;
            Parity = settings.Parity;
            StopBits = settings.StopBits;
            Handshake = settings.Handshake;
        }

        private void UpdateHostAndPortFromPortName(string portName)
        {
            var hostAndPort = portName.ToLower().Replace("tcp://", "").Split(':');

            RemoteHost = hostAndPort[0];
            RemotePort = int.Parse(hostAndPort[1]);
        }
    }
}
