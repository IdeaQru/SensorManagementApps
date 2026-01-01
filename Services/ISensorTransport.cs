using System;

namespace SensorMonitorDesktop.Services
{
    public interface ISensorTransport : IDisposable
    {
        event Action<string> DataReceived;
        void Start();
        void Stop();
        bool IsConnected { get; }
    }
}
