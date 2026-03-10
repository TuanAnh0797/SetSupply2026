using APP;
using Microsoft.Extensions.DependencyInjection;
using SetSupply.Helper;
using SetSupply.ViewModels.UserControlViewModels.History;
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
using static MaterialDesignThemes.Wpf.Theme;

namespace SetSupply.UserControls.History
{
    /// <summary>
    /// Interaction logic for UCHistory.xaml
    /// </summary>
    public partial class UCHistory : UserControl
    {
        private readonly UCHistoryViewModel _viewModel;

        public UCHistory()
        {
            InitializeComponent();
            _viewModel = App.ServiceProvider.GetRequiredService<UCHistoryViewModel>();
            DataContext = _viewModel;

            this.Loaded += UCHistory_Loaded;
            this.Unloaded += UCHistory_Unloaded;
        }

        private async void UCHistory_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel.DataLoaded += OnDataLoaded;

            await _viewModel.InitializeAsync();

            RegenerateAllColumns();
        }

        private void UCHistory_Unloaded(object sender, RoutedEventArgs e)
        {
            // Hủy đăng ký event
            _viewModel.DataLoaded -= OnDataLoaded;

            // Clear DataGrid trước khi unload để tránh lỗi FindAncestor
            DataGridColumnHelper.ClearDataGrid(Shift1DataGrid);
            DataGridColumnHelper.ClearDataGrid(Shift2DataGrid);
            DataGridColumnHelper.ClearDataGrid(Shift3DataGrid);
        }

        private void OnDataLoaded()
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
            {
                RegenerateAllColumns();
            });
        }

        private void RegenerateAllColumns()
        {
            DataGridColumnHelper.GenerateShiftColumns(Shift1DataGrid, _viewModel.Shift1);
            DataGridColumnHelper.GenerateShiftColumns(Shift2DataGrid, _viewModel.Shift2);
            DataGridColumnHelper.GenerateShiftColumns(Shift3DataGrid, _viewModel.Shift3);
        }
    }
}