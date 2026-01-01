using System.Windows.Controls;
using SensorMonitorDesktop.ViewModels;

namespace SensorMonitorDesktop.Views
{
    public partial class MonitoringView : UserControl
    {
        public MonitoringView()
        {
            InitializeComponent();
            DataContext = new MonitoringViewModel();
        }
    }
}
