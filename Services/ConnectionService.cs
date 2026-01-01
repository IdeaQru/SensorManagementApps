namespace SensorMonitorDesktop.Services
{
    public static class ConnectionService
    {
        private static ISensorTransport? _currentTransport;

        public static void SetTransport(ISensorTransport transport)
        {
            _currentTransport?.Dispose();
            _currentTransport = transport;
        }

        public static ISensorTransport? GetTransport()
        {
            return _currentTransport;
        }

        public static void Disconnect()
        {
            _currentTransport?.Dispose();
            _currentTransport = null;
        }
    }
}
