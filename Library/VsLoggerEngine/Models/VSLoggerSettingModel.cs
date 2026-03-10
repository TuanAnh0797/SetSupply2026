using VSLibrary.Common.MVVM.ViewModels;

namespace VsLoggerEngine.Models;

public class VSLoggerSettingModel : ViewModelBase
{
    public static readonly string LogLineRegex = @"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}) \| (DEBUG|INFO|WARN|ERROR) \| (.*?) \| (.*?) \| (.+?) \| (\w+) \| (\d+)$";
    private string _logFolderPath = string.Empty;
    private int _maxNumberOfLogLine = 10000;
    private bool _isStayOnTop = true;

    public string LogFolderPath
    {
        get => _logFolderPath;
        set => SetProperty(ref _logFolderPath, value);
    }

    public int MaxNumberOfLogLine
    {
        get => _maxNumberOfLogLine;
        set => SetProperty(ref _maxNumberOfLogLine, value);
    }

    public bool IsStayOnTop
    {
        get => _isStayOnTop;
        set => SetProperty(ref _isStayOnTop, value);
    }
}
