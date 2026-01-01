using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using SensorMonitorDesktop.Services;

namespace SensorMonitorDesktop.ViewModels
{
    public enum ConnectionType
    {
        Serial,
        Network
    }

    public enum NetworkMode
    {
        TCP,
        UDP
    }

    public class ConnectionViewModel : INotifyPropertyChanged
    {
        // Connection Type
        public ObservableCollection<ConnectionType> ConnectionTypes { get; }
        public ObservableCollection<NetworkMode> NetworkModes { get; }

        private ConnectionType _selectedConnectionType = ConnectionType.Serial;
        public ConnectionType SelectedConnectionType
        {
            get => _selectedConnectionType;
            set 
            { 
                _selectedConnectionType = value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSerialMode));
                OnPropertyChanged(nameof(IsNetworkMode));
            }
        }

        public bool IsSerialMode => SelectedConnectionType == ConnectionType.Serial;
        public bool IsNetworkMode => SelectedConnectionType == ConnectionType.Network;

        // Serial Port Settings
        public ObservableCollection<string> AvailablePorts { get; }
        public ObservableCollection<int> BaudRates { get; }
        public ObservableCollection<Parity> ParityOptions { get; }
        public ObservableCollection<int> DataBitsOptions { get; }
        public ObservableCollection<StopBits> StopBitsOptions { get; }

        private string? _selectedPort;
        public string? SelectedPort
        {
            get => _selectedPort;
            set { _selectedPort = value; OnPropertyChanged(); }
        }

        private int _selectedBaudRate = 9600;
        public int SelectedBaudRate
        {
            get => _selectedBaudRate;
            set { _selectedBaudRate = value; OnPropertyChanged(); }
        }

        private Parity _selectedParity = Parity.None;
        public Parity SelectedParity
        {
            get => _selectedParity;
            set { _selectedParity = value; OnPropertyChanged(); }
        }

        private int _selectedDataBits = 8;
        public int SelectedDataBits
        {
            get => _selectedDataBits;
            set { _selectedDataBits = value; OnPropertyChanged(); }
        }

        private StopBits _selectedStopBits = StopBits.One;
        public StopBits SelectedStopBits
        {
            get => _selectedStopBits;
            set { _selectedStopBits = value; OnPropertyChanged(); }
        }

        // Network Settings
        private NetworkMode _selectedNetworkMode = NetworkMode.TCP;
        public NetworkMode SelectedNetworkMode
        {
            get => _selectedNetworkMode;
            set { _selectedNetworkMode = value; OnPropertyChanged(); }
        }

        private string _host = "127.0.0.1";
        public string Host
        {
            get => _host;
            set { _host = value; OnPropertyChanged(); }
        }

        private int _port = 5000;
        public int Port
        {
            get => _port;
            set { _port = value; OnPropertyChanged(); }
        }

        // Status
        private string _statusText = "Disconnected";
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set 
            { 
                _isConnected = value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDisconnected));
            }
        }

        public bool IsDisconnected => !IsConnected;

        // Commands
        public ICommand RefreshPortsCommand { get; }
        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }

        public ConnectionViewModel()
        {
            // Initialize collections
            ConnectionTypes = new ObservableCollection<ConnectionType>
            {
                ConnectionType.Serial,
                ConnectionType.Network
            };

            NetworkModes = new ObservableCollection<NetworkMode>
            {
                NetworkMode.TCP,
                NetworkMode.UDP
            };

            AvailablePorts = new ObservableCollection<string>();
            
            BaudRates = new ObservableCollection<int>
            {
                4800, 9600, 19200, 38400, 57600, 115200, 230400
            };

            ParityOptions = new ObservableCollection<Parity>
            {
                Parity.None,
                Parity.Even,
                Parity.Odd,
                Parity.Mark,
                Parity.Space
            };

            DataBitsOptions = new ObservableCollection<int>
            {
                5, 6, 7, 8
            };

            StopBitsOptions = new ObservableCollection<StopBits>
            {
                StopBits.None,
                StopBits.One,
                StopBits.Two,
                StopBits.OnePointFive
            };

            // Initialize commands
            RefreshPortsCommand = new RelayCommand(_ => RefreshPorts());
            ConnectCommand = new RelayCommand(_ => Connect(), _ => CanConnect());
            DisconnectCommand = new RelayCommand(_ => Disconnect(), _ => IsConnected);

            // Load available ports
            RefreshPorts();
        }

        private void RefreshPorts()
        {
            try
            {
                AvailablePorts.Clear();
                var ports = SerialPort.GetPortNames();
                
                foreach (var port in ports)
                {
                    AvailablePorts.Add(port);
                }

                if (AvailablePorts.Count > 0 && SelectedPort == null)
                {
                    SelectedPort = AvailablePorts[0];
                }

                StatusText = $"Found {AvailablePorts.Count} serial port(s)";
            }
            catch (Exception ex)
            {
                StatusText = $"Error refreshing ports: {ex.Message}";
            }
        }

        private bool CanConnect()
        {
            if (IsConnected) return false;

            if (SelectedConnectionType == ConnectionType.Serial)
            {
                return !string.IsNullOrWhiteSpace(SelectedPort);
            }
            else
            {
                return Port > 0 && Port <= 65535;
            }
        }

        private void Connect()
        {
            try
            {
                ISensorTransport? transport = null;

                if (SelectedConnectionType == ConnectionType.Serial)
                {
                    // Validate serial settings
                    if (string.IsNullOrWhiteSpace(SelectedPort))
                    {
                        MessageBox.Show("Please select a COM port", 
                                       "Validation Error", 
                                       MessageBoxButton.OK, 
                                       MessageBoxImage.Warning);
                        return;
                    }

                    // Create serial transport
                    transport = new SerialTransport(
                        SelectedPort,
                        SelectedBaudRate,
                        SelectedParity,
                        SelectedDataBits,
                        SelectedStopBits
                    );

                    transport.Start();
                    ConnectionService.SetTransport(transport);

                    IsConnected = true;
                    StatusText = $"✅ Connected: {SelectedPort} @ {SelectedBaudRate} baud";

                    MessageBox.Show(
                        $"Serial port connected successfully!\n\n" +
                        $"Port: {SelectedPort}\n" +
                        $"Baud Rate: {SelectedBaudRate}\n" +
                        $"Data Format: {SelectedDataBits}{SelectedParity.ToString()[0]}{(int)SelectedStopBits}",
                        "Connection Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else // Network
                {
                    // Validate network settings
                    if (Port <= 0 || Port > 65535)
                    {
                        MessageBox.Show("Port must be between 1 and 65535", 
                                       "Validation Error", 
                                       MessageBoxButton.OK, 
                                       MessageBoxImage.Warning);
                        return;
                    }

                    // Create network transport
                    transport = SelectedNetworkMode switch
                    {
                        NetworkMode.TCP => new TcpTransport(Port),
                        NetworkMode.UDP => new UdpTransport(Port),
                        _ => throw new NotSupportedException($"Network mode {SelectedNetworkMode} not supported")
                    };

                    transport.Start();
                    ConnectionService.SetTransport(transport);

                    IsConnected = true;
                    StatusText = $"✅ Listening: {SelectedNetworkMode} on port {Port}";

                    MessageBox.Show(
                        $"{SelectedNetworkMode} server started successfully!\n\n" +
                        $"Protocol: {SelectedNetworkMode}\n" +
                        $"Port: {Port}\n" +
                        $"Host: {Host}\n\n" +
                        $"Waiting for incoming connections...",
                        "Server Started",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (UnauthorizedAccessException)
            {
                StatusText = "❌ Access denied - port may be in use";
                MessageBox.Show(
                    "Cannot access the port.\n\n" +
                    "The port may be:\n" +
                    "• Already in use by another application\n" +
                    "• Blocked by antivirus/firewall\n" +
                    "• Requires administrator privileges\n\n" +
                    "Try:\n" +
                    "• Closing other applications using this port\n" +
                    "• Running the application as Administrator\n" +
                    "• Choosing a different port",
                    "Connection Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (IOException ioEx)
            {
                StatusText = $"❌ I/O Error: {ioEx.Message}";
                MessageBox.Show(
                    $"I/O Error occurred:\n\n{ioEx.Message}\n\n" +
                    $"Please check:\n" +
                    $"• Device is properly connected\n" +
                    $"• Correct port is selected\n" +
                    $"• Port settings match device configuration",
                    "Connection Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                StatusText = $"❌ Error: {ex.Message}";
                MessageBox.Show(
                    $"Failed to connect:\n\n{ex.Message}\n\n{ex.StackTrace}",
                    "Connection Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Disconnect()
        {
            try
            {
                var transport = ConnectionService.GetTransport();
                
                if (transport != null)
                {
                    var connectionInfo = SelectedConnectionType == ConnectionType.Serial
                        ? $"{SelectedPort}"
                        : $"{SelectedNetworkMode} port {Port}";

                    ConnectionService.Disconnect();
                    IsConnected = false;
                    StatusText = $"⏸ Disconnected from {connectionInfo}";

                    MessageBox.Show(
                        $"Disconnected successfully from:\n{connectionInfo}",
                        "Disconnected",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    IsConnected = false;
                    StatusText = "Not connected";
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Error disconnecting: {ex.Message}";
                MessageBox.Show(
                    $"Error during disconnect:\n\n{ex.Message}",
                    "Disconnect Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
