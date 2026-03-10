using VsLoggerEngine.Models;
using VsLoggerEngine.ViewModels;
using System.Windows;

namespace VsLoggerEngine.Views;

/// <summary>
/// Interaction logic for LoggerOptionWindow.xaml
/// </summary>
public partial class LoggerOptionsWindow : Window
{
    public LoggerOptionsWindow( VSLoggerSettingModel vsLoggerSetting)
    {
        InitializeComponent();
        DataContext = new LoggerOptionsViewModel(vsLoggerSetting);
    }
}
