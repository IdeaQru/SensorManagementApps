using System.Windows.Controls;
using SensorMonitorDesktop.ViewModels;

namespace SensorMonitorDesktop.Views
{
    public partial class SensorView : UserControl
    {
        public SensorView()
        {
            InitializeComponent();
            DataContext = new SensorViewModel();
        }
    }
}


