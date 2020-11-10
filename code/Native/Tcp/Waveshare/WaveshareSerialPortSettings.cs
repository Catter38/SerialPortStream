namespace RJCP.IO.Ports.Native.Tcp.Waveshare
{
    public class WaveshareSerialPortSettings : TcpSerialPortSettings
    {
        public int UartPacketTime { get; set; } = 0;

        public int UartPacketLength { get; set; } = 0;

        public bool SyncBaudRate { get; set; } = true;
    }
}