using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using SensorMonitorDesktop.Data;
using SensorMonitorDesktop.Models;

namespace SensorMonitorDesktop.ViewModels
{
    public class SensorTypeViewModel : INotifyPropertyChanged
    {
        private readonly AppDbContext _db;
        public ObservableCollection<SensorType> Types { get; set; }
        
        public ObservableCollection<string> AvailableDataTypes { get; set; }
        
        private SensorType _currentType = new();
        public SensorType CurrentType
        {
            get => _currentType;
            set { _currentType = value; OnPropertyChanged(); }
        }

        public ICommand NewCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }  // ✅ Tambah ini

        public SensorTypeViewModel()
        {
            _db = new AppDbContext();
            Types = new ObservableCollection<SensorType>();
            
            AvailableDataTypes = new ObservableCollection<string>
            {
                "float",
                "int",
                "bool",
                "string"
            };

            NewCommand = new RelayCommand(_ => CurrentType = new SensorType());
            SaveCommand = new RelayCommand(_ => SaveType());
            DeleteCommand = new RelayCommand(_ => DeleteType(), _ => CurrentType?.Id > 0);  // ✅ Tambah ini

            _ = LoadTypesAsync();
        }

        private async Task LoadTypesAsync()
        {
            var list = await _db.SensorTypes.ToListAsync();
            Types.Clear();
            foreach (var t in list)
                Types.Add(t);
        }

        private void SaveType()
        {
            if (string.IsNullOrWhiteSpace(CurrentType.Name))
            {
                MessageBox.Show("Name is required!", "Validation Error", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CurrentType.Id == 0)
                _db.SensorTypes.Add(CurrentType);
            else
                _db.SensorTypes.Update(CurrentType);

            _db.SaveChanges();
            _ = LoadTypesAsync();
        }

        // ✅ Method untuk delete
        private void DeleteType()
        {
            if (CurrentType?.Id > 0)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete '{CurrentType.Name}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _db.SensorTypes.Remove(CurrentType);
                        _db.SaveChanges();
                        CurrentType = new SensorType();
                        _ = LoadTypesAsync();
                        
                        MessageBox.Show("Sensor Type deleted successfully!", 
                                      "Success", 
                                      MessageBoxButton.OK, 
                                      MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Cannot delete: {ex.Message}\n\nThis type might be used by sensors.", 
                                      "Delete Error", 
                                      MessageBoxButton.OK, 
                                      MessageBoxImage.Error);
                    }
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
