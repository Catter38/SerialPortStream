namespace RJCP.IO.Ports.Native
{
    public class SerialPortSettings
    {
        public string PortName { get; set; }

        public int BaudRate { get; set; }

        public int DataBits { get; set; }

        public Parity Parity { get; set; }

        public StopBits StopBits { get; set; }

        public Handshake Handshake { get; set; } = Handshake.None;
    }
}
