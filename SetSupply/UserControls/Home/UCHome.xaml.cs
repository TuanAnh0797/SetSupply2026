
using APP.ViewModels.UserControlViewModels.Home;
using Microsoft.Extensions.DependencyInjection;
using SetSupply.Helper;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace APP.UserControls;

public partial class UCHome : UserControl
{
    private readonly UCHomeViewModel _viewModel;

    public UCHome()
    {
        InitializeComponent();
        _viewModel = App.ServiceProvider.GetRequiredService<UCHomeViewModel>();
        DataContext = _viewModel;

        this.Loaded += UCHome_Loaded;
        this.Unloaded += UCHome_Unloaded;
    }

    private async void UCHome_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();

        DataGridColumnHelper.GenerateShiftColumns(Shift1DataGrid, _viewModel.Shift1);
        DataGridColumnHelper.GenerateShiftColumns(Shift2DataGrid, _viewModel.Shift2);
        DataGridColumnHelper.GenerateShiftColumns(Shift3DataGrid, _viewModel.Shift3);
    }

    private void UCHome_Unloaded(object sender, RoutedEventArgs e)
    {
        // Clear DataGrid trước khi unload để tránh lỗi FindAncestor
        DataGridColumnHelper.ClearDataGrid(Shift1DataGrid);
        DataGridColumnHelper.ClearDataGrid(Shift2DataGrid);
        DataGridColumnHelper.ClearDataGrid(Shift3DataGrid);
    }
}
