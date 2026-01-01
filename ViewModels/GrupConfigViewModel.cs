using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using SensorMonitorDesktop.Data;
using SensorMonitorDesktop.Models;
using Microsoft.EntityFrameworkCore; 
namespace SensorMonitorDesktop.ViewModels
{
    public class GroupConfigViewModel : INotifyPropertyChanged
    {
        private readonly AppDbContext _db;

        private SensorGroup _currentGroup = new SensorGroup();

        public ObservableCollection<SensorGroup> Groups { get; } = new();

        public SensorGroup CurrentGroup
        {
            get => _currentGroup;
            set
            {
                _currentGroup = value;
                OnPropertyChanged();
            }
        }

        public ICommand NewGroupCommand { get; }
        public ICommand SaveGroupCommand { get; }

        public GroupConfigViewModel()
        {
            _db = new AppDbContext();

            NewGroupCommand = new RelayCommand(_ => NewGroup());
            SaveGroupCommand = new RelayCommand(async _ => await SaveGroupAsync(), _ => CanSaveGroup());

            _ = LoadGroupsAsync();
        }

        private async Task LoadGroupsAsync()
        {
            Groups.Clear();
            var list = await _db.SensorGroups.ToListAsync();
            foreach (var g in list)
            {
                Groups.Add(g);
            }
        }

        private void NewGroup()
        {
            CurrentGroup = new SensorGroup();
        }

        private bool CanSaveGroup()
        {
            return !string.IsNullOrWhiteSpace(CurrentGroup.Name);
        }

        private async Task SaveGroupAsync()
        {
            if (CurrentGroup.Id == 0)
            {
                _db.SensorGroups.Add(CurrentGroup);
            }
            else
            {
                _db.SensorGroups.Update(CurrentGroup);
            }

            await _db.SaveChangesAsync();
            await LoadGroupsAsync();
            NewGroup();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
