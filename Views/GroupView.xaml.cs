using System.Windows.Controls;
using SensorMonitorDesktop.ViewModels;

namespace SensorMonitorDesktop.Views
{
    public partial class GroupView : UserControl
    {
        public GroupView()
        {
            InitializeComponent();
            DataContext = new GroupConfigViewModel();
        }
    }
}
