using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SensorMonitorDesktop.Services
{
    public class UdpTransport : ISensorTransport
    {
        private UdpClient? _udpClient;
        private CancellationTokenSource? _cts;
        private readonly int _port;

        public event Action<string>? DataReceived;
        public bool IsConnected => _udpClient != null;

        public UdpTransport(int port)
        {
            _port = port;
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _udpClient = new UdpClient(_port);
            
            Task.Run(() => ReceiveDataAsync(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
            _udpClient?.Close();
            _udpClient = null;
        }

        private async Task ReceiveDataAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && _udpClient != null)
                {
                    var result = await _udpClient.ReceiveAsync();
                    var data = Encoding.UTF8.GetString(result.Buffer);

                    // UDP typically sends complete messages
                    var lines = data.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            DataReceived?.Invoke(line.Trim());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UDP receive error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
        }
    }
}
