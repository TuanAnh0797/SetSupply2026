using APP.ViewModels.UserControlViewModels.Home;
using APP.ViewModels.UserControlViewModels.Setting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace APP.UserControls
{
    /// <summary>
    /// Interaction logic for UCSetting.xaml
    /// </summary>
    public partial class UCSetting : UserControl
    {
        private UCSettingViewModel _viewModel;
        public UCSetting()
        {
            InitializeComponent();
            _viewModel = App.ServiceProvider.GetRequiredService<UCSettingViewModel>();
            DataContext = _viewModel;
        }
    }
}
