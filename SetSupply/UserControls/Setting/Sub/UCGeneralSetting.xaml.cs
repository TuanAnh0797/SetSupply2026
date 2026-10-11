using APP;
using Microsoft.Extensions.DependencyInjection;
using SetSupply.ViewModels.UserControlViewModels.Setting.Sub;
using System.Windows.Controls;

namespace SetSupply.UserControls.Setting.Sub;

public partial class UCGeneralSetting : UserControl
{
    private readonly UCGeneralSettingViewModel _viewModel;

    public UCGeneralSetting()
    {
        InitializeComponent();
        _viewModel = App.ServiceProvider.GetRequiredService<UCGeneralSettingViewModel>();
        DataContext = _viewModel;

        Loaded += (_, _) => _viewModel.Load();
    }
}
