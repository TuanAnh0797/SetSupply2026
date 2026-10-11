using APP;
using Microsoft.Extensions.DependencyInjection;
using SetSupply.ViewModels.UserControlViewModels.Setting.Sub;
using System.Windows.Controls;

namespace SetSupply.UserControls.Setting.Sub;

public partial class UCDailyPlanSetting : UserControl
{
    private readonly UCDailyPlanSettingViewModel _viewModel;

    public UCDailyPlanSetting()
    {
        InitializeComponent();
        _viewModel = App.ServiceProvider.GetRequiredService<UCDailyPlanSettingViewModel>();
        DataContext = _viewModel;

        Loaded += async (_, _) => await _viewModel.Load();
    }
}
