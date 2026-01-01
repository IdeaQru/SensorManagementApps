using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SensorMonitorDesktop.Services
{
    public class TcpTransport : ISensorTransport
    {
        private TcpListener? _listener;
        private TcpClient? _client;
        private CancellationTokenSource? _cts;
        private readonly int _port;
        private readonly StringBuilder _buffer = new();

        public event Action<string>? DataReceived;
        public bool IsConnected => _client?.Connected ?? false;

        public TcpTransport(int port)
        {
            _port = port;
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();

            Task.Run(() => AcceptClientsAsync(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
            
            _client?.Close();
            _client = null;
            
            _listener?.Stop();
            _listener = null;
        }

        private async Task AcceptClientsAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && _listener != null)
                {
                    _client = await _listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(_client, token), token);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TCP accept error: {ex.Message}");
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            try
            {
                using var stream = client.GetStream();
                var buffer = new byte[1024];

                while (!token.IsCancellationRequested && client.Connected)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                    
                    if (bytesRead == 0) break; // Client disconnected

                    var data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    _buffer.Append(data);

                    // Process complete lines
                    var bufferStr = _buffer.ToString();
                    var lines = bufferStr.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    if (bufferStr.EndsWith('\n') || bufferStr.EndsWith('\r'))
                    {
                        foreach (var line in lines)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                DataReceived?.Invoke(line.Trim());
                            }
                        }
                        _buffer.Clear();
                    }
                    else if (lines.Length > 1)
                    {
                        for (int i = 0; i < lines.Length - 1; i++)
                        {
                            if (!string.IsNullOrWhiteSpace(lines[i]))
                            {
                                DataReceived?.Invoke(lines[i].Trim());
                            }
                        }
                        _buffer.Clear();
                        _buffer.Append(lines[^1]);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TCP client error: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
        }
    }
}
