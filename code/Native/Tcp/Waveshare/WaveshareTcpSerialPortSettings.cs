namespace RJCP.IO.Ports.Native.Tcp.Waveshare
{
    public class WaveshareTcpSerialPortSettings : TcpSerialPortSettings
    {
        public WaveshareTcpSerialPortSettings(string username, string password)
        {
            SerialPortSettingsManager = new WaveshareTcpSerialSettingsManager(RemoteHost, RemotePort, username, password);
        }
    }
}
