using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;

namespace RJCP.IO.Ports.Native.Tcp.Waveshare
{
    public class WaveshareTcpSerialSettingsManager : ITcpSerialPortSettingsManager
    {
        private readonly string m_Host;

        private readonly int m_Port;

        private readonly string m_Username;

        private readonly string m_Password;

        public WaveshareTcpSerialSettingsManager(string host, int port, string username, string password)
        {
            m_Host = host;
            m_Port = port;
            m_Username = username;
            m_Password = password;
        }

        public bool SetSettings(TcpSerialPortSettings settings)
        {
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(m_Username + ":" + m_Password));
            var parity = ((int) settings.Parity) + 1;
            var flowMode = GetFlowModeFromHandshake(settings.Handshake);

            var url = $"http://{m_Host}/config.cgi" +
                      $"?br={settings.BaudRate}" +                  // baudrate
                      $"&bc={settings.DataBits}" +                  // data bits
                      $"&parity={parity}" +                         // parity
                      $"&stop={settings.StopBits}" +                // stop bits
                      "&flow=1" +                                   // Serial mode (1=RS232; 5=RS485; 6=RS422; 8=Dial-Switch)
                      $"&xon={flowMode}" +                          // flow control (0=NONE; 3=RTS/CTS; 32=XON/XOFF)
                      $"&tim={settings.UartPacketTime}" +           // UART Packet time
                      $"&num={settings.UartPacketLength}" +         // UART Packet length
                      $"&srf={(settings.SyncBaudRate ? 1 : 0)}" +   // sync baud rate
                      "&hebt=0123456789" +                          // Uart Heartbeat packet
                      "&srm=1" +                                    // Uart Heartbeat ASCII
                      "&srz=30" +                                   // Uart Heartbeat time
                      "&tnmode=3" +                                 // working mode (0=UDP-Client; 1=TCP-Client; 2=UDP-Server; 3=TCP-Server; 4=HTTPD-Client)
                      "&mbtp=0" +                                   // modbus type
                      "&tcpstx=9" +                                 // TCP max. sockets (2=1; 3=2; 4=3; 5=4; 6=5; 7=6; 8=7; 9=8)
                      "&ticken=0" +                                 // Action on max. sockets (0=KICK; 1=KEEP)
                      "&urh=16" +                                   // httpd type (16=GET; 32=POST)
                      "&urf=1" +                                    // remove httpd head
                      "&url=%2F1.php%3F" +                          // httpd URL(<100byte)
                      "&hhr=User_Agent%3A+Mozilla%2F4.0%0D%0A" +    // httpd Client Header(<180byte)
                      $"&tlp={m_Port}" +                             // local/remote port
                      "&srh=86400" +                                // timeout reconnection (1-99999s)
                      "&srq=3" +                                    // disconnect time (2-255s)
                      "&ura=10" +                                   // server response time (2-255s)
                      "&hebn=0123456789" +                          // net haertbeat packet
                      "&srp=1" +                                    // net heartbeat ASCII
                      "&srr=30" +                                   // net heartbeat time (1-65535)
                      "&sru=0" +                                    // registry type (0=None; 1=USER-Register; 2=USR-Cloud; 4=MAC-As-Register)
                      "&regt=0123456789" +                          // net registry packet
                      "&srt=1" +                                    // net registry packet ASCII
                      "&lde0=" +                                    // USR-Cloud Device ID
                      "&lpa0=" +                                    // USR-Cloud Communications Code
                      "&tnbode=7" +                                 // Socket B Work mode (0=UDP-Client; 1=TCP-Client; 7=NONE)
                      "&urb1=192.168.0.201" +                       // Socket B Remote Server address
                      "&trb=20105";                                 // Socket B Remote Port number

            var client = new WebClient { Headers = { [HttpRequestHeader.Authorization] = "Basic " + credentials } };

            if (DownloadString(client, url)) // update settings in device
            {
                if (DownloadString(client, $"http://{m_Host}/login.cgi")) // reboot device
                {
                    Thread.Sleep(1000);

                    return DownloadString(client, $"http://{m_Host}/"); // verify if online again
                }
            }

            return false;
        }

        private int GetFlowModeFromHandshake(Handshake handshake)
        {
            switch (handshake)
            {
                case Handshake.None:
                    return 0;

                case Handshake.XOn:
                    return 3;

                case Handshake.Rts:
                    return 32;

                default:
                    return 0;
            }
        }

        private bool DownloadString(WebClient webClient, string url, int retries = 5)
        {
            var error = true;

            while (error && retries > 0)
            {
                try
                {
                    var result = webClient.DownloadString(url);

                    Debug.WriteLine(result);

                    error = false;
                }
                catch (Exception ex)
                {
                    if (ex is WebException webException)
                    {
                        var r = (HttpWebResponse)webException.Response;

                        if (r.StatusCode == HttpStatusCode.NotFound)
                        {
                            error = false;
                        }
                        else
                        {
                            Thread.Sleep(500);
                        }
                    }
                    else
                    {
                        Thread.Sleep(500);
                    }
                }

                retries--;
            }

            return !error;
        }
    }
}
