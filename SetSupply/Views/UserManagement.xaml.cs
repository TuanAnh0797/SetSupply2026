using APP.ViewModels.FormViewModels;
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
using System.Windows.Shapes;

namespace APP.Views
{
    /// <summary>
    /// Interaction logic for UserManagement.xaml
    /// </summary>
    public partial class UserManagement : Window
    {
        private UserManagementViewModel _viewModel;
        public UserManagement()
        {
            InitializeComponent();
            _viewModel = App.ServiceProvider.GetRequiredService<UserManagementViewModel>();
            DataContext = _viewModel;

        }
    }
}
