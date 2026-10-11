using APP;
using Microsoft.Extensions.DependencyInjection;
using SetSupply.ViewModels.UserControlViewModels.Setting.Sub;
using System.Windows.Controls;

namespace SetSupply.UserControls.Setting.Sub;

public partial class UCModelGroupSetting : UserControl
{
    private readonly UCModelGroupSettingViewModel _viewModel;

    public UCModelGroupSetting()
    {
        InitializeComponent();
        _viewModel = App.ServiceProvider.GetRequiredService<UCModelGroupSettingViewModel>();
        DataContext = _viewModel;

        Loaded += async (_, _) => await _viewModel.Load();
    }
}
