using System.Windows;
using SensorMonitorDesktop.ViewModels;

namespace SensorMonitorDesktop
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new GroupConfigViewModel();
        }

      
    }
}
