using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NoteBar.Core.Udp
{
    public class UdpServer
    {
        public static Action<string> LogAction { get; set; }

        private UdpClient _udpClientV4;
        private UdpClient _udpClientV6;
        private Action<string> GetMessageAction { get; }

        public uint Port { get; }

        public UdpServer(uint port, Action<string> getMessageAction)
        {
            Port = port;
            GetMessageAction = getMessageAction;

            // 1. Explicitly bind IPv4 to 0.0.0.0
            try
            {
                _udpClientV4 = new UdpClient(new IPEndPoint(IPAddress.Any, (int)port));
                LogAction?.Invoke($"[UdpServer:{port}] Bound to IPv4 0.0.0.0:{port}");
            }
            catch (Exception ex)
            {
                LogAction?.Invoke($"[UdpServer:{port}] IPv4 0.0.0.0 bind error: {ex.Message}");
            }

            // 2. Also bind IPv6 to [::] if supported
            try
            {
                _udpClientV6 = new UdpClient(AddressFamily.InterNetworkV6);
                _udpClientV6.Client.DualMode = false;
                _udpClientV6.Client.Bind(new IPEndPoint(IPAddress.IPv6Any, (int)port));
                LogAction?.Invoke($"[UdpServer:{port}] Bound to IPv6 [::]:{port}");
            }
            catch (Exception ex)
            {
                _udpClientV6?.Dispose();
                _udpClientV6 = null;
                LogAction?.Invoke($"[UdpServer:{port}] IPv6 bind skipped: {ex.Message}");
            }
        }

        public void Start()
        {
            BeginReceive(_udpClientV4);
            BeginReceive(_udpClientV6);
        }

        private void BeginReceive(UdpClient client)
        {
            if (client == null) return;
            try
            {
                client.BeginReceive(HandleData, client);
            }
            catch (Exception ex)
            {
                LogAction?.Invoke($"[UdpServer:{Port}] BeginReceive error: {ex.Message}");
            }
        }

        private void HandleData(IAsyncResult result)
        {
            var client = result.AsyncState as UdpClient;
            if (client == null) return;

            var iPEndPoint = new IPEndPoint(IPAddress.Any, 0);

            try
            {
                var data = client.EndReceive(result, ref iPEndPoint);
                var message = Encoding.UTF8.GetString(data);
                LogAction?.Invoke($"[UdpServer:{Port}] Received {data.Length} bytes from {iPEndPoint}: '{message}'");
                GetMessageAction?.Invoke(message);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception ex)
            {
                LogAction?.Invoke($"[UdpServer:{Port}] HandleData exception: {ex.Message}");
            }

            try
            {
                if (client.Client != null)
                {
                    BeginReceive(client);
                }
            }
            catch (Exception ex)
            {
                LogAction?.Invoke($"[UdpServer:{Port}] Restart BeginReceive error: {ex.Message}");
            }
        }

        public void ShutDown()
        {
            try
            {
                _udpClientV4?.Close();
                _udpClientV6?.Close();
                LogAction?.Invoke($"[UdpServer:{Port}] Sockets closed");
            }
            catch (Exception ex)
            {
                LogAction?.Invoke($"[UdpServer:{Port}] ShutDown exception: {ex.Message}");
            }
        }
    }
}
