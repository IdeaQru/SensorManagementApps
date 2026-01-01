using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using SensorMonitorDesktop.Data;
using SensorMonitorDesktop.Models;
using SensorMonitorDesktop.Services;

namespace SensorMonitorDesktop.ViewModels
{
    public class LiveReadingDto : INotifyPropertyChanged
    {
        private string _value = "";
        private DateTime _timestamp;
        private string _status = "";

        public int SensorId { get; set; }
        public string SensorName { get; set; } = "";
        public string SensorType { get; set; } = "";

        public string Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); }
        }

        public string Unit { get; set; } = "";

        public DateTime Timestamp
        {
            get => _timestamp;
            set { _timestamp = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class MonitoringViewModel : INotifyPropertyChanged
    {
        private readonly AppDbContext _db;
        private readonly DispatcherTimer _timer;
        private ISensorTransport? _transport;

        public ObservableCollection<SensorGroup> Groups { get; }
        public ObservableCollection<LiveReadingDto> LiveReadings { get; }

        private SensorGroup? _selectedGroup;
        public SensorGroup? SelectedGroup
        {
            get => _selectedGroup;
            set
            {
                _selectedGroup = value;
                OnPropertyChanged();
                if (_timer.IsEnabled)
                {
                    LiveReadings.Clear();
                }
            }
        }

        private int _refreshIntervalMs = 1000;
        public int RefreshIntervalMs
        {
            get => _refreshIntervalMs;
            set
            {
                if (value < 100) value = 100;
                if (value > 10000) value = 10000;

                _refreshIntervalMs = value;
                OnPropertyChanged();
                _timer.Interval = TimeSpan.FromMilliseconds(_refreshIntervalMs);
            }
        }

        private bool _isMonitoring;
        public bool IsMonitoring
        {
            get => _isMonitoring;
            set { _isMonitoring = value; OnPropertyChanged(); }
        }

        private string _statusText = "Ready";
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        private int _receivedCount = 0;
        public int ReceivedCount
        {
            get => _receivedCount;
            set { _receivedCount = value; OnPropertyChanged(); }
        }

        private int _matchedCount = 0;
        public int MatchedCount
        {
            get => _matchedCount;
            set { _matchedCount = value; OnPropertyChanged(); }
        }

        public ICommand StartMonitoringCommand { get; }
        public ICommand StopMonitoringCommand { get; }
        public ICommand ClearReadingsCommand { get; }

        public MonitoringViewModel()
        {
            _db = new AppDbContext();
            Groups = new ObservableCollection<SensorGroup>();
            LiveReadings = new ObservableCollection<LiveReadingDto>();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(_refreshIntervalMs)
            };
            _timer.Tick += Timer_Tick;

            StartMonitoringCommand = new RelayCommand(
                _ => StartMonitoring(),
                _ => !IsMonitoring && SelectedGroup != null);

            StopMonitoringCommand = new RelayCommand(
                _ => StopMonitoring(),
                _ => IsMonitoring);

            ClearReadingsCommand = new RelayCommand(
                _ => ClearReadings(),
                _ => LiveReadings.Any());

            _ = LoadGroupsAsync();
        }

        private async Task LoadGroupsAsync()
        {
            try
            {
                var list = await _db.SensorGroups
                    .AsNoTracking()
                    .OrderBy(g => g.Name)
                    .ToListAsync();

                Groups.Clear();
                foreach (var g in list)
                    Groups.Add(g);

                SelectedGroup = Groups.FirstOrDefault();

                System.Diagnostics.Debug.WriteLine($"[MonitoringVM] Loaded {list.Count} groups");
            }
            catch (Exception ex)
            {
                StatusText = $"Error loading groups: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[MonitoringVM] LoadGroups Error: {ex.Message}");
            }
        }

        private void StartMonitoring()
        {
            if (SelectedGroup == null)
            {
                MessageBox.Show("Please select a sensor group first.",
                               "No Group Selected",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return;
            }

            System.Diagnostics.Debug.WriteLine($"\n========================================");
            System.Diagnostics.Debug.WriteLine($"[MonitoringVM] START MONITORING");
            System.Diagnostics.Debug.WriteLine($"[MonitoringVM] Group: {SelectedGroup.Name} (ID: {SelectedGroup.Id})");
            System.Diagnostics.Debug.WriteLine($"========================================");

            // Get transport dari ConnectionService
            _transport = ConnectionService.GetTransport();

            if (_transport == null)
            {
                System.Diagnostics.Debug.WriteLine("[MonitoringVM] ❌ No transport available!");

                MessageBox.Show(
                    "No connection configured!\n\n" +
                    "Please go to Connection tab and:\n" +
                    "1. Choose Serial or Network mode\n" +
                    "2. Click Connect\n" +
                    "3. Return to Monitoring tab\n" +
                    "4. Start monitoring",
                    "Connection Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            System.Diagnostics.Debug.WriteLine("[MonitoringVM] ✅ Transport found, subscribing to DataReceived...");

            // Subscribe to data events
            _transport.DataReceived += OnDataReceived;

            // List all sensors yang akan di-monitor
            var sensors = _db.Sensors
                .Include(s => s.SensorType)
                .Where(s => s.GroupId == SelectedGroup.Id && s.IsActive)
                .ToList();

            System.Diagnostics.Debug.WriteLine($"[MonitoringVM] Monitoring {sensors.Count} active sensors:");
            foreach (var s in sensors)
            {
                System.Diagnostics.Debug.WriteLine($"  - ID:{s.Id} | Name:'{s.Name}' | Address:'{s.Address}' | Type:{s.SensorType.Name}");
            }
            System.Diagnostics.Debug.WriteLine($"========================================\n");

            IsMonitoring = true;
            ReceivedCount = 0;
            MatchedCount = 0;
            StatusText = $"🔴 LIVE - Waiting for data from {SelectedGroup.Name}...";

            _timer.Start();
        }

        private void StopMonitoring()
        {
            _timer.Stop();

            if (_transport != null)
            {
                System.Diagnostics.Debug.WriteLine("[MonitoringVM] Unsubscribing from transport...");
                _transport.DataReceived -= OnDataReceived;
                _transport = null;
            }

            IsMonitoring = false;
            StatusText = $"⏸ Stopped | Total: {ReceivedCount} received, {MatchedCount} matched";
            System.Diagnostics.Debug.WriteLine($"[MonitoringVM] Monitoring stopped. Received: {ReceivedCount}, Matched: {MatchedCount}");
        }

        private void ClearReadings()
        {
            LiveReadings.Clear();
            StatusText = "Cleared readings";
        }

        // Handler untuk data dari Serial/Network
        // Handler untuk data dari Serial/Network
        private void OnDataReceived(string rawData)
        {
            try
            {
                ReceivedCount++;
                System.Diagnostics.Debug.WriteLine($"\n[MonitoringVM] ===== Data Received #{ReceivedCount} =====");
                System.Diagnostics.Debug.WriteLine($"[MonitoringVM] Raw: '{rawData}'");

                // Format expected: "Address:Value"
                // Contoh: "A1:25.5" atau "CH1:78.3"
                var parts = rawData.Split(':');
                if (parts.Length != 2)
                {
                    System.Diagnostics.Debug.WriteLine($"[MonitoringVM] ❌ Invalid format (expected 'Address:Value')");
                    return;
                }

                var address = parts[0].Trim();  // ✅ Hanya ambil address
                var valueStr = parts[1].Trim();

                System.Diagnostics.Debug.WriteLine($"[MonitoringVM] Parsed → Address: '{address}', Value: '{valueStr}'");
                System.Diagnostics.Debug.WriteLine($"[MonitoringVM] Looking for sensor with Address='{address}' in Group ID: {SelectedGroup?.Id}");

                // ✅ PERBAIKAN: HANYA match berdasarkan Address dan GroupId
                var sensor = _db.Sensors
                    .Include(s => s.SensorType)
                    .Include(s => s.Group)
                    .AsNoTracking()
                    .FirstOrDefault(s =>
                        s.GroupId == SelectedGroup!.Id &&
                        s.Address == address &&  // ✅ HANYA cek Address, bukan Name
                        s.IsActive);

                if (sensor == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[MonitoringVM] ❌ Sensor NOT FOUND!");
                    System.Diagnostics.Debug.WriteLine($"[MonitoringVM] Searched for Address='{address}' in Group={SelectedGroup!.Name} (ID:{SelectedGroup.Id})");

                    // List available sensors
                    var allSensors = _db.Sensors
                        .Where(s => s.GroupId == SelectedGroup!.Id)
                        .Select(s => new { s.Id, s.Name, s.Address, s.IsActive })
                        .ToList();

                    System.Diagnostics.Debug.WriteLine($"[MonitoringVM] Available addresses in group '{SelectedGroup.Name}':");
                    foreach (var s in allSensors)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - Address:'{s.Address}' → {s.Name} (Active:{s.IsActive})");
                    }
                    System.Diagnostics.Debug.WriteLine($"[MonitoringVM] ⚠️ Data ignored (address not found in this group)\n");
                    return;
                }

                MatchedCount++;
                System.Diagnostics.Debug.WriteLine($"[MonitoringVM] ✅ Sensor FOUND! (Match #{MatchedCount})");
                System.Diagnostics.Debug.WriteLine($"[MonitoringVM]    Address: '{sensor.Address}'");
                System.Diagnostics.Debug.WriteLine($"[MonitoringVM]    Name: '{sensor.Name}'");
                System.Diagnostics.Debug.WriteLine($"[MonitoringVM]    Type: {sensor.SensorType.Name}");
                System.Diagnostics.Debug.WriteLine($"[MonitoringVM]    Group: {sensor.Group.Name}");

                var now = DateTime.Now;
                var status = DetermineStatus(sensor.SensorType.DataType, valueStr);

                System.Diagnostics.Debug.WriteLine($"[MonitoringVM]    Status: {status}");

                // Update UI (must be on UI thread)
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var existing = LiveReadings.FirstOrDefault(r => r.SensorId == sensor.Id);
                    if (existing == null)
                    {
                        existing = new LiveReadingDto
                        {
                            SensorId = sensor.Id,
                            SensorName = sensor.Name,
                            SensorType = sensor.SensorType.Name,
                            Unit = sensor.SensorType.Unit ?? ""
                        };
                        LiveReadings.Add(existing);
                        System.Diagnostics.Debug.WriteLine($"[MonitoringVM] ✅ Added new row to LiveReadings");
                    }

                    existing.Value = valueStr;
                    existing.Timestamp = now;
                    existing.Status = status;

                    System.Diagnostics.Debug.WriteLine($"[MonitoringVM] ✅ Updated UI: {sensor.Name} = {valueStr} @ {now:HH:mm:ss}\n");
                });

                // Save to database (background)
                Task.Run(() =>
                {
                    try
                    {
                        using var db = new AppDbContext();
                        db.SensorReadings.Add(new SensorReading
                        {
                            SensorId = sensor.Id,
                            Timestamp = now,
                            Value = valueStr,
                            Status = status
                        });
                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MonitoringVM] ❌ DB Save Error: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MonitoringVM] ❌ OnDataReceived Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MonitoringVM] StackTrace: {ex.StackTrace}");
            }
        }

        // Timer hanya untuk update status display
        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (IsMonitoring && _transport != null)
            {
                StatusText = $"🔴 LIVE | Sensors: {LiveReadings.Count} | Received: {ReceivedCount} | Matched: {MatchedCount} | {DateTime.Now:HH:mm:ss}";
            }
        }

        private string DetermineStatus(string dataType, string value)
        {
            if (dataType.ToLower() == "float" || dataType.ToLower() == "int")
            {
                if (double.TryParse(value, out double numValue))
                {
                    if (numValue > 95) return "ALARM";
                    if (numValue > 90) return "WARNING";
                }
            }
            return "OK";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
