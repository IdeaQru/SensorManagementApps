using System.Windows.Controls;
using SensorMonitorDesktop.ViewModels;

namespace SensorMonitorDesktop.Views
{
    public partial class SensorTypeView : UserControl
    {
        public SensorTypeView()
        {
            InitializeComponent();
            DataContext = new SensorTypeViewModel();
        }
    }
}
