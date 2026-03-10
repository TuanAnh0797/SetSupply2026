using APP;
using APP.ViewModels.UserControlViewModels.Setting;
using APP.ViewModels.UserControlViewModels.Setting.Sub;
using Microsoft.Extensions.DependencyInjection;
using SetSupply.ViewModels.UserControlViewModels.Setting.Sub;
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

namespace SetSupply.UserControls.Setting.Sub
{
    /// <summary>
    /// Interaction logic for UCShiftSetting.xaml
    /// </summary>
    public partial class UCShiftSetting : UserControl
    {
        private UCShiftSettingViewModel _viewModel;
        public UCShiftSetting()
        {
            InitializeComponent();
            _viewModel = App.ServiceProvider.GetRequiredService<UCShiftSettingViewModel>();
            DataContext = _viewModel;
        }
    }
}
