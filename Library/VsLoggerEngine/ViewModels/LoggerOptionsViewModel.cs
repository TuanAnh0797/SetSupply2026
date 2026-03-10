using CommunityToolkit.Mvvm.Input;
using VsLoggerEngine.Models;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Input;
using VSLibrary.Common.MVVM.ViewModels;

namespace VsLoggerEngine.ViewModels;

public class LoggerOptionsViewModel : ViewModelBase
{
    public string CallerAppName { get; }

    public VSLoggerSettingModel VSLoggerSetting { get; }

    public ICommand SetLogFolderPathCommand { get; set; }

    public LoggerOptionsViewModel(VSLoggerSettingModel vsLoggerSetting)
    {
        CallerAppName = App.CallerAppName;
        VSLoggerSetting = vsLoggerSetting;

        SetLogFolderPathCommand = new RelayCommand<Window>(OnSetLogFolderPath);
    }

    private void OnSetLogFolderPath(Window? window)
    {
        var dialog = new OpenFolderDialog();
        dialog.Multiselect = false;

        var result = dialog.ShowDialog();

        if (result == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
        {
            VSLoggerSetting.LogFolderPath = dialog.FolderName;
        }
    }
}
