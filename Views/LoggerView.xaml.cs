using System.Windows.Controls;
using SensorMonitorDesktop.ViewModels;

namespace SensorMonitorDesktop.Views
{
    public partial class LoggerView : UserControl
    {
        public LoggerView()
        {
            InitializeComponent();
            DataContext = new LoggerViewModel();
        }
    }
}
