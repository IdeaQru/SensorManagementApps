using System;
using System.IO.Ports;
using System.Text;

namespace SensorMonitorDesktop.Services
{
    public class SerialTransport : ISensorTransport
    {
        private SerialPort? _serialPort;
        private readonly StringBuilder _buffer = new();

        public event Action<string>? DataReceived;
        public bool IsConnected => _serialPort?.IsOpen ?? false;

        public SerialTransport(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            _serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits)
            {
                Encoding = Encoding.UTF8,
                ReadTimeout = 500,
                WriteTimeout = 500
            };

            _serialPort.DataReceived += SerialPort_DataReceived;
        }

        public void Start()
        {
            if (_serialPort != null && !_serialPort.IsOpen)
            {
                _serialPort.Open();
            }
        }

        public void Stop()
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (_serialPort == null || !_serialPort.IsOpen) return;

                var data = _serialPort.ReadExisting();
                _buffer.Append(data);

                // Process complete lines (terminated by \n or \r\n)
                var bufferStr = _buffer.ToString();
                var lines = bufferStr.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                if (bufferStr.EndsWith('\n') || bufferStr.EndsWith('\r'))
                {
                    // Complete line(s) received
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
                    // Multiple lines, last one incomplete
                    for (int i = 0; i < lines.Length - 1; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(lines[i]))
                        {
                            DataReceived?.Invoke(lines[i].Trim());
                        }
                    }
                    _buffer.Clear();
                    _buffer.Append(lines[^1]); // Keep last incomplete line
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Serial read error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_serialPort != null)
            {
                _serialPort.DataReceived -= SerialPort_DataReceived;
                
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
                
                _serialPort.Dispose();
                _serialPort = null;
            }
        }
    }
}
