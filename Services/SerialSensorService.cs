using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;

namespace SensorMonitorDesktop.Services
{
    public class SerialSensorService : IDisposable
    {
        private readonly SerialPort _port;
        private readonly Thread _readThread;
        private bool _running;

        // Event saat data sensor diterima
        public event Action<string>? LineReceived;

        public SerialSensorService(string portName, int baudRate = 9600)
        {
            _port = new SerialPort(portName, baudRate)
            {
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                NewLine = "\n"
            };

            _readThread = new Thread(ReadLoop) { IsBackground = true };
        }

        public void Start()
        {
            if (_running) return;

            _port.Open();
            _running = true;
            _readThread.Start();
        }

        public void Stop()
        {
            _running = false;
            if (_port.IsOpen)
                _port.Close();
        }

        private void ReadLoop()
        {
            try
            {
                while (_running)
                {
                    string? line = _port.ReadLine(); // blok sampai ada data
                    if (!string.IsNullOrWhiteSpace(line))
                        LineReceived?.Invoke(line.Trim());
                }
            }
            catch (Exception)
            {
                // bisa ditambah logging/error handling
            }
        }

        public void Dispose()
        {
            Stop();
            _port.Dispose();
        }
    }
}
