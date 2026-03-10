using APP;
using Microsoft.Extensions.DependencyInjection;
using SetSupply.ViewModels.UserControlViewModels.DashBoard;
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

namespace SetSupply.UserControls.DashBoard
{
    /// <summary>
    /// Interaction logic for UCDashboard.xaml
    /// </summary>
    public partial class UCDashboard : UserControl
    {
        private readonly UCDashboardViewModel _viewModel;

        public UCDashboard()
        {
            InitializeComponent();
            _viewModel = App.ServiceProvider.GetRequiredService<UCDashboardViewModel>();
            DataContext = _viewModel;

            this.Loaded += UCDashboard_Loaded;
            this.Unloaded += UCDashboard_Unloaded;
        }

        private async void UCDashboard_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitializeAsync();
        }

        private void UCDashboard_Unloaded(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
