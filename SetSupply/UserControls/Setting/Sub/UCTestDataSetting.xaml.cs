using APP;
using Microsoft.Extensions.DependencyInjection;
using SetSupply.ViewModels.UserControlViewModels.Setting.Sub;
using System.Windows.Controls;

namespace SetSupply.UserControls.Setting.Sub;

public partial class UCTestDataSetting : UserControl
{
    private readonly UCTestDataSettingViewModel _viewModel;

    public UCTestDataSetting()
    {
        InitializeComponent();
        _viewModel = App.ServiceProvider.GetRequiredService<UCTestDataSettingViewModel>();
        DataContext = _viewModel;

        Loaded += async (_, _) => await _viewModel.Load();
    }
}
