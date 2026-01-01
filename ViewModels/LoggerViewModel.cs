using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using SensorMonitorDesktop.Data;
using SensorMonitorDesktop.Models;

namespace SensorMonitorDesktop.ViewModels
{
    public class LogRowDto
    {
        public string GroupName { get; set; } = "";
        public string SensorName { get; set; } = "";
        public string SensorType { get; set; } = "";
        public string Value { get; set; } = "";  // ✅ Ubah dari double ke string
        public string Unit { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = "";
    }

    public class LoggerViewModel : INotifyPropertyChanged
    {
        private readonly AppDbContext _db;

        public ObservableCollection<SensorGroup> Groups { get; }
        public ObservableCollection<LogRowDto> Logs { get; }

        private string _sessionName = "";
        public string SessionName
        {
            get => _sessionName;
            set { _sessionName = value; OnPropertyChanged(); }
        }

        private SensorGroup? _selectedGroup;
        public SensorGroup? SelectedGroup
        {
            get => _selectedGroup;
            set { _selectedGroup = value; OnPropertyChanged(); }
        }

        private DateTime _fromDate = DateTime.Today.AddDays(-1);
        public DateTime FromDate
        {
            get => _fromDate;
            set { _fromDate = value; OnPropertyChanged(); }
        }

        private DateTime _toDate = DateTime.Today;
        public DateTime ToDate
        {
            get => _toDate;
            set { _toDate = value; OnPropertyChanged(); }
        }

        private string _statusText = "Ready";
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public ICommand LoadLogsCommand { get; }
        public ICommand ExportCsvCommand { get; }
        public ICommand ClearLogsCommand { get; }  // ✅ Tambah command clear

        public LoggerViewModel()
        {
            _db = new AppDbContext();
            Groups = new ObservableCollection<SensorGroup>();
            Logs = new ObservableCollection<LogRowDto>();

            LoadLogsCommand = new RelayCommand(async _ => await LoadLogsAsync());
            ExportCsvCommand = new RelayCommand(_ => ExportCsv(), _ => Logs.Any());
            ClearLogsCommand = new RelayCommand(_ => ClearLogs(), _ => Logs.Any());

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
                Groups.Add(new SensorGroup { Id = 0, Name = "-- All Groups --" });  // ✅ Option untuk semua
                
                foreach (var g in list)
                    Groups.Add(g);

                SelectedGroup = Groups.FirstOrDefault();
            }
            catch (Exception ex)
            {
                StatusText = $"Error loading groups: {ex.Message}";
            }
        }

        private async Task LoadLogsAsync()
        {
            try
            {
                Logs.Clear();
                StatusText = "Loading...";

                var from = FromDate.Date;
                var to = ToDate.Date.AddDays(1).AddSeconds(-1);  // ✅ End of day

                if (from > to)
                {
                    MessageBox.Show("'From Date' must be before 'To Date'", 
                                   "Invalid Date Range", 
                                   MessageBoxButton.OK, 
                                   MessageBoxImage.Warning);
                    StatusText = "Invalid date range";
                    return;
                }

                var query = _db.SensorReadings
                    .Include(r => r.Sensor)
                        .ThenInclude(s => s.Group)
                    .Include(r => r.Sensor)
                        .ThenInclude(s => s.SensorType)
                    .Where(r => r.Timestamp >= from && r.Timestamp <= to)
                    .AsNoTracking();

                // ✅ Filter by group jika bukan "All Groups"
                if (SelectedGroup != null && SelectedGroup.Id > 0)
                    query = query.Where(r => r.Sensor.GroupId == SelectedGroup.Id);

                var list = await query
                    .OrderByDescending(r => r.Timestamp)  // ✅ Newest first
                    .Take(10000)  // ✅ Limit untuk performa
                    .ToListAsync();

                foreach (var r in list)
                {
                    Logs.Add(new LogRowDto
                    {
                        GroupName = r.Sensor?.Group?.Name ?? "N/A",
                        SensorName = r.Sensor?.Name ?? "N/A",
                        SensorType = r.Sensor?.SensorType?.Name ?? "N/A",
                        Value = r.Value ?? "",  // ✅ String value
                        Unit = r.Sensor?.SensorType?.Unit ?? "",
                        Timestamp = r.Timestamp,
                        Status = r.Status ?? "OK"
                    });
                }

                StatusText = $"Loaded {Logs.Count} record(s) from {from:yyyy-MM-dd} to {to:yyyy-MM-dd}";
            }
            catch (Exception ex)
            {
                StatusText = $"Error: {ex.Message}";
                MessageBox.Show($"Failed to load logs:\n{ex.Message}", 
                               "Error", 
                               MessageBoxButton.OK, 
                               MessageBoxImage.Error);
            }
        }

        private void ExportCsv()
        {
            try
            {
                var fileName = string.IsNullOrWhiteSpace(SessionName)
                    ? $"sensor_logs_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                    : $"{SanitizeFileName(SessionName)}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                var path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    fileName);

                var sb = new StringBuilder();
                sb.AppendLine("Group,Sensor,Type,Value,Unit,Timestamp,Status");

                foreach (var row in Logs)
                {
                    sb.AppendLine(
                        $"{Escape(row.GroupName)}," +
                        $"{Escape(row.SensorName)}," +
                        $"{Escape(row.SensorType)}," +
                        $"{Escape(row.Value)}," +  // ✅ Value sudah string
                        $"{Escape(row.Unit)}," +
                        $"{row.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                        $"{Escape(row.Status)}");
                }

                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                
                StatusText = $"Exported {Logs.Count} rows to Desktop";
                MessageBox.Show($"Successfully exported to:\n{path}", 
                               "Export Complete", 
                               MessageBoxButton.OK, 
                               MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusText = $"Export failed: {ex.Message}";
                MessageBox.Show($"Export failed:\n{ex.Message}", 
                               "Error", 
                               MessageBoxButton.OK, 
                               MessageBoxImage.Error);
            }
        }

        private void ClearLogs()
        {
            var result = MessageBox.Show(
                $"Clear {Logs.Count} log entries from view?\n(This will not delete database records)",
                "Clear Logs",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Logs.Clear();
                StatusText = "Logs cleared from view";
            }
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            
            return value;
        }

        private static string SanitizeFileName(string fileName)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
