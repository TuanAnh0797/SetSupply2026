using CommunityToolkit.Mvvm.Input;
using VsLoggerEngine.Constants;
using VsLoggerEngine.Extensions;
using VsLoggerEngine.Models;
using VsLoggerEngine.Views;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using VSLibrary.Common.MVVM.ViewModels;

namespace VsLoggerEngine.ViewModels;

public class LoggerViewModel : ViewModelBase
{
    private static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "VisionSemicon",
        "VSLogger"
    );

    private FileSystemWatcher? _logWatcher;
    private DateTime _logDate = DateTime.Now;
    private bool _isMonitoring;
    private long _lastPosition = 0;
    private int _lineIndex = 0;
    private int _infoCount;
    private int _errorCount;
    private int _warningCount;
    private int _debugCount;
    private bool _hasData;
    private string _currentLogFile;

    public string CallerAppName { get; }

    public VSLoggerSettingModel VSLoggerSetting { get; set; } = new();

    public ObservableCollection<LogItem> Logs { get; } = new();

    public ICollectionView FilteredLogs { get; private set; }

    public DateTime LogDate
    {
        get => _logDate;
        set => SetProperty(ref _logDate, value);
    }

    public bool IsMonitoring
    {
        get => _isMonitoring;
        set => SetProperty(ref _isMonitoring, value);
    }

    public int InfoCount
    {
        get => _infoCount;
        set => SetProperty(ref _infoCount, value);
    }

    public int ErrorCount
    {
        get => _errorCount;
        set => SetProperty(ref _errorCount, value);
    }

    public int WarningCount
    {
        get => _warningCount;
        set => SetProperty(ref _warningCount, value);
    }

    public int DebugCount
    {
        get => _debugCount;
        set => SetProperty(ref _debugCount, value);
    }

    public bool HasData
    {
        get => _hasData;
        set => SetProperty(ref _hasData, value);
    }

    public string CurrentLogFile
    {
        get => _currentLogFile;
        set => SetProperty(ref _currentLogFile, value);
    }

    public LogFilterModel LogFilter { get; } = new();
    private DispatcherTimer _filterTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };

    public ICommand StartStopCommand { get; }
    public ICommand OpenLoggerOptionsCommand { get; }
    public ICommand OpenLogFileCommand { get; }

    public LoggerViewModel()
    {
        CallerAppName = App.CallerAppName;

        StartStopCommand = new RelayCommand<Window>(OnStartStop);
        OpenLoggerOptionsCommand = new RelayCommand<Window>(OnOpenLoggerOptions);
        OpenLogFileCommand = new RelayCommand<Window>(OnOpenLogFile);

        LoadSettings();
        OnLogFileChanged();
        VSLoggerSetting.PropertyChanged += VSLoggerSetting_PropertyChanged;

        FilteredLogs = CollectionViewSource.GetDefaultView(Logs);
        FilteredLogs.Filter = BuildFilterQuery;
        SortingLogs();

        LogFilter.PropertyChanged += LogFilter_PropertyChanged;

        PropertyChanged += LoggerViewModel_PropertyChanged;
    }

    private void LoggerViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LogDate))
        {
            OnLogFileChanged();
        }
    }

    private void LogFilter_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _filterTimer.Stop();
        _filterTimer.Tick -= FilterTimer_Tick;
        _filterTimer.Tick += FilterTimer_Tick;
        _filterTimer.Start();
    }

    private void FilterTimer_Tick(object? sender, EventArgs e)
    {
        _filterTimer.Stop();
        FilteredLogs.Filter = BuildFilterQuery;
    }

    private bool BuildFilterQuery(object obj)
    {
        if (obj is not LogItem item)
            return false;

        var msgFilter = LogFilter.SearchMessage is { } searchMessage && !string.IsNullOrWhiteSpace(searchMessage) ?
            (
                item.LogMessage.Contains(searchMessage, StringComparison.OrdinalIgnoreCase) ||
                item.FilePath.Contains(searchMessage, StringComparison.OrdinalIgnoreCase) ||
                item.FunctionName.Contains(searchMessage, StringComparison.OrdinalIgnoreCase)
            )
            : true;

        var allLevelVisible = LogFilter.AllLevelVisible;
        var infoLevelVisible = LogFilter.IsInfoVisible ? item.LogLevel == LogLevel.INFO : true && allLevelVisible;
        var warnLevelVisible = LogFilter.IsWarnVisible ? item.LogLevel == LogLevel.WARN : true && allLevelVisible;
        var errorLevelVisible = LogFilter.IsErrorVisible ? item.LogLevel == LogLevel.ERROR : true && allLevelVisible;
        var debugLevelVisible = LogFilter.IsDebugVisible ? item.LogLevel == LogLevel.DEBUG : true && allLevelVisible;

        var fromTimeVisible = item.LogTime.TimeOfDay.TrimToHhMm() >= LogFilter.FromTime.TimeOfDay.TrimToHhMm();
        var toTimeVisible = item.LogTime.TimeOfDay.TrimToHhMm() <= LogFilter.ToTime.TimeOfDay.TrimToHhMm();

        return msgFilter && (infoLevelVisible || warnLevelVisible || errorLevelVisible || debugLevelVisible) && (fromTimeVisible && toTimeVisible);
    }

    private void LoadSettings()
    {
        var settingFile = Path.Combine(AppDataPath, $"VSLoggerSetting_{CallerAppName}.json");
        if (!File.Exists(settingFile))
            VSLoggerSetting = new VSLoggerSettingModel();
        try
        {
            string json = File.ReadAllText(settingFile);
            VSLoggerSetting = JsonConvert.DeserializeObject<VSLoggerSettingModel>(json) ?? new VSLoggerSettingModel();
        }
        catch
        {
            VSLoggerSetting = new VSLoggerSettingModel();
        }

        if (!string.IsNullOrEmpty(App.LogFolderPath))
        {
            VSLoggerSetting.LogFolderPath = App.LogFolderPath;
        }
    }

    private void SaveSetting()
    {
        if (!Directory.Exists(AppDataPath))
            Directory.CreateDirectory(AppDataPath);

        string json = JsonConvert.SerializeObject(VSLoggerSetting, Formatting.Indented);
        var settingFile = Path.Combine(AppDataPath, $"VSLoggerSetting_{CallerAppName}.json");
        File.WriteAllText(settingFile, json);
    }

    private void VSLoggerSetting_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VSLoggerSetting.LogFolderPath))
        {
            OnLogFileChanged();
        }

        if (e.PropertyName == nameof(VSLoggerSetting.IsStayOnTop))
        {
            SortingLogs();
        }

        SaveSetting();
    }

    private void SortingLogs()
    {
        FilteredLogs.SortDescriptions.Clear();
        FilteredLogs.SortDescriptions.Add(new SortDescription(nameof(LogItem.Index),
            VSLoggerSetting.IsStayOnTop ? ListSortDirection.Descending : ListSortDirection.Ascending));
    }

    private void OnStartStop(Window? window)
    {
        IsMonitoring = !IsMonitoring;

        if (IsMonitoring)
        {
            StartLogWatcher(window);
        }
        else
        {
            StopLogWatcher();
        }
    }

    private void StartLogWatcher(Window? window)
    {
        var logFolderPath = VSLoggerSetting.LogFolderPath;
        if (string.IsNullOrEmpty(logFolderPath))
        {
            IsMonitoring = false;
            MsgBox.ShowDialog(window, "Log folder path is not set. Please set it in the options.", msgBoxType: MessageBoxType.Warning);
            return;
        }

        Logs.Clear();
        ClearLogCounts();

        LogFilter.ResetFilter();
        LogDate = DateTime.Now;
        LogFilter.FromTime = DateTime.Today + DateTime.Now.TimeOfDay.TrimToHhMm();

        var logFileName = $"VsLog_{LogDate:yyyyMMdd}.log";
        var logFilePath = Path.Combine(logFolderPath, logFileName);
        CurrentLogFile = logFilePath;

        _lastPosition = File.Exists(logFilePath) ? new FileInfo(logFilePath).Length : 0;

        _logWatcher ??= new FileSystemWatcher(logFolderPath, logFileName)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite
        };

        _logWatcher.Changed += (s, e) =>
        {
            Task.Run(() =>
            {
                try
                {
                    using var fs = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(fs);

                    fs.Seek(_lastPosition, SeekOrigin.Begin);
                    var newLogs = new List<LogItem>();

                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (ParseLogLine(line) is { } logItem)
                        {
                            newLogs.Add(logItem);
                        }
                    }

                    _lastPosition = fs.Position;

                    if (newLogs.Any())
                    {
                        System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                        {
                            foreach (var logItem in newLogs)
                            {
                                Logs.Add(logItem);
                                while (Logs.Count > VSLoggerSetting.MaxNumberOfLogLine)
                                {
                                    Logs.RemoveAt(0);
                                }
                                UpdateLogCounts();
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            });
        };

        _logWatcher.EnableRaisingEvents = true;
    }

    private LogItem? ParseLogLine(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        var regex = new Regex(VSLoggerSettingModel.LogLineRegex);
        var match = regex.Match(line);
        if (!match.Success)
            return null;

        try
        {
            var timeStr = match.Groups[1].Value;
            var levelStr = match.Groups[2].Value;
            var prefix = match.Groups[3].Value;
            var message = match.Groups[4].Value;
            var filePath = match.Groups[5].Value;
            var function = match.Groups[6].Value;
            var invokeline = int.TryParse(match.Groups[7].Value, out var val) ? val : 0;

            if (!DateTime.TryParse(timeStr, out var logTime))
                return null;

            var logLevel = levelStr.ToUpperInvariant() switch
            {
                "INFO" => LogLevel.INFO,
                "WARN" => LogLevel.WARN,
                "ERROR" => LogLevel.ERROR,
                _ => LogLevel.DEBUG
            };
            _lineIndex++;
            HasData = true;
            return new LogItem(_lineIndex, logLevel, logTime, prefix, message, filePath, function, invokeline);
        }
        catch
        {
            return null;
        }
    }

    private void StopLogWatcher()
    {
        if (_logWatcher == null) return;
        _logWatcher.EnableRaisingEvents = false;

        OnLogFileChanged();
    }

    public void OnOpenLoggerOptions(Window? window)
    {
        new LoggerOptionsWindow(VSLoggerSetting)
        {
            Owner = window,
        }.ShowDialog();
    }

    private void OnOpenLogFile(Window? window)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Open Log File",
            Filter = "Log files (*.log)|*.log",
            DefaultExt = ".log",
            CheckFileExists = true,
            Multiselect = false
        };

        bool? result = dialog.ShowDialog();
        if (result == true)
        {
            var filePath = dialog.FileName;
            if (File.Exists(filePath))
            {
                PropertyChanged -= LoggerViewModel_PropertyChanged;

                LogDate = File.GetLastWriteTime(filePath);

                OnLogFileChanged(filePath);

                PropertyChanged += LoggerViewModel_PropertyChanged;
            }
        }
    }

    private void OnLogFileChanged(string? filePath = null)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            _lineIndex = 0;
            HasData = false;

            ClearLogCounts();
            Logs.Clear();

            LogFilter.ResetFilter();

            var logFilePath = filePath ?? Path.Combine(VSLoggerSetting.LogFolderPath, $"VsLog_{LogDate:yyyyMMdd}.log");
            CurrentLogFile = logFilePath;

            if (!File.Exists(logFilePath))
                return;

            try
            {
                using var fs = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs);

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var item = ParseLogLine(line);
                    if (item != null)
                    {
                        Logs.Add(item.Value);
                    }
                }

                UpdateLogCounts();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        });
    }

    private void ClearLogCounts()
    {
        InfoCount = 0;
        ErrorCount = 0;
        WarningCount = 0;
        DebugCount = 0;
    }

    private void UpdateLogCounts()
    {
        InfoCount = Logs.Count(x => x.LogLevel == LogLevel.INFO);
        WarningCount = Logs.Count(x => x.LogLevel == LogLevel.WARN);
        ErrorCount = Logs.Count(x => x.LogLevel == LogLevel.ERROR);
        DebugCount = Logs.Count(x => x.LogLevel == LogLevel.DEBUG || (x.LogLevel != LogLevel.INFO && x.LogLevel != LogLevel.WARN && x.LogLevel != LogLevel.ERROR));
    }
}
