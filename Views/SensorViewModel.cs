using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using SensorMonitorDesktop.Data;
using SensorMonitorDesktop.Models;

namespace SensorMonitorDesktop.ViewModels
{
    public class SensorViewModel : INotifyPropertyChanged
    {
        private readonly AppDbContext _db;

        public ObservableCollection<SensorGroup> Groups { get; } = new();
        public ObservableCollection<SensorType> SensorTypes { get; } = new();
        public ObservableCollection<Sensor> Sensors { get; } = new();

        private SensorGroup? _selectedGroup;
        public SensorGroup? SelectedGroup
        {
            get => _selectedGroup;
            set
            {
                _selectedGroup = value;
                OnPropertyChanged();
                if (CurrentSensor != null && _selectedGroup != null)
                    CurrentSensor.GroupId = _selectedGroup.Id;
            }
        }

        private SensorType? _selectedType;
        public SensorType? SelectedType
        {
            get => _selectedType;
            set
            {
                _selectedType = value;
                OnPropertyChanged();
                if (CurrentSensor != null && _selectedType != null)
                    CurrentSensor.SensorTypeId = _selectedType.Id;
            }
        }

        private Sensor _currentSensor = new Sensor();
        public Sensor CurrentSensor
        {
            get => _currentSensor;
            set
            {
                _currentSensor = value;
                OnPropertyChanged();
                SelectedGroup = _currentSensor.Group;
                SelectedType = _currentSensor.SensorType;
            }
        }

        public ICommand NewSensorCommand { get; }
        public ICommand SaveSensorCommand { get; }

        public SensorViewModel()
        {
            _db = new AppDbContext();

            NewSensorCommand = new RelayCommand(_ => NewSensor());
            SaveSensorCommand = new RelayCommand(async _ => await SaveSensorAsync(), _ => CanSaveSensor());

            _ = LoadLookupsAsync();
            _ = LoadSensorsAsync();
        }

        private async Task LoadLookupsAsync()
        {
            Groups.Clear();
            SensorTypes.Clear();

            var groups = await _db.SensorGroups.ToListAsync();
            var types  = await _db.SensorTypes.ToListAsync();

            foreach (var g in groups) Groups.Add(g);
            foreach (var t in types)  SensorTypes.Add(t);
        }

        private async Task LoadSensorsAsync()
        {
            Sensors.Clear();
            var list = await _db.Sensors
                                .Include(s => s.Group)
                                .Include(s => s.SensorType)
                                .ToListAsync();
            foreach (var s in list)
                Sensors.Add(s);
        }

        private void NewSensor()
        {
            CurrentSensor = new Sensor { IsActive = true };
            SelectedGroup = null;
            SelectedType = null;
        }

        private bool CanSaveSensor()
        {
            return CurrentSensor != null
                   && !string.IsNullOrWhiteSpace(CurrentSensor.Name)
                   && SelectedGroup != null
                   && SelectedType != null;
        }

        private async Task SaveSensorAsync()
        {
            if (CurrentSensor.Id == 0)
                _db.Sensors.Add(CurrentSensor);
            else
                _db.Sensors.Update(CurrentSensor);

            await _db.SaveChangesAsync();
            await LoadSensorsAsync();
            NewSensor();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
