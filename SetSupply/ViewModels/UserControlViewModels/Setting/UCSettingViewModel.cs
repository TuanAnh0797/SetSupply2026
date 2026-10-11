using APP.UserControls;
using APP.UserControls.Setting.Sub;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using SetSupply.Models.Database;
using SetSupply.UserControls.Setting.Sub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace APP.ViewModels.UserControlViewModels.Setting;

public partial class UCSettingViewModel : ObservableObject
{
    [ObservableProperty]
    private UserControl currentView;
    public UCSettingViewModel()
    {
        CurrentView = App.ServiceProvider.GetRequiredService<UCPLCSetting>();
    }
    [RelayCommand]
    private void ShowPLCSetting()
    {
        CurrentView = App.ServiceProvider.GetRequiredService<UCPLCSetting>();
    }
    [RelayCommand]
    private void ShowCalendar()
    {
        CurrentView = App.ServiceProvider.GetRequiredService<UCShiftSetting>();
    }
    [RelayCommand]
    private void ShowModelGroupSetting()
    {
        CurrentView = App.ServiceProvider.GetRequiredService<UCModelGroupSetting>();
    }
    [RelayCommand]
    private void ShowTestDataSetting()
    {
        CurrentView = App.ServiceProvider.GetRequiredService<UCTestDataSetting>();
    }
    [RelayCommand]
    private void ShowGeneralSetting()
    {
        CurrentView = App.ServiceProvider.GetRequiredService<UCGeneralSetting>();
    }
    [RelayCommand]
    private void ShowDailyPlanSetting()
    {
        CurrentView = App.ServiceProvider.GetRequiredService<UCDailyPlanSetting>();
    }

}
