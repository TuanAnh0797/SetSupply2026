using APP.ViewModels.UserControlViewModels.Setting;
using APP.ViewModels.UserControlViewModels.Setting.Sub;
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

namespace APP.UserControls.Setting.Sub
{
    /// <summary>
    /// Interaction logic for UCPLCSetting.xaml
    /// </summary>
    public partial class UCPLCSetting : UserControl
    {
        private UCPLCSettingViewModel _viewModel;
        public UCPLCSetting()
        {
            InitializeComponent();
            _viewModel = App.ServiceProvider.GetRequiredService<UCPLCSettingViewModel>();
            DataContext = _viewModel;
        }
    }
}
