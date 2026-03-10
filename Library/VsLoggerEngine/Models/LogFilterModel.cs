using VSLibrary.Common.MVVM.ViewModels;

namespace VsLoggerEngine.Models;

public class LogFilterModel : ViewModelBase
{
    private string? _searchMessage;

    private bool _isInfoVisible = false;
    private bool _isWarnVisible = false;
    private bool _isErrorVisible = false;
    private bool _isDebugVisible = false;

    private DateTime _fromTime = DateTime.Today;
    private DateTime _toTime = DateTime.Today + new TimeSpan(23, 59, 59);

    public string? SearchMessage
    {
        get => _searchMessage;
        set => SetProperty(ref _searchMessage, value);
    }

    public bool IsInfoVisible
    {
        get => _isInfoVisible;
        set => SetProperty(ref _isInfoVisible, value);
    }

    public bool IsWarnVisible
    {
        get => _isWarnVisible;
        set => SetProperty(ref _isWarnVisible, value);
    }

    public bool IsErrorVisible
    {
        get => _isErrorVisible;
        set => SetProperty(ref _isErrorVisible, value);
    }

    public bool IsDebugVisible
    {
        get => _isDebugVisible;
        set => SetProperty(ref _isDebugVisible, value);
    }

    public bool AllLevelVisible => IsInfoVisible && IsWarnVisible && IsErrorVisible && IsDebugVisible || !IsInfoVisible && !IsWarnVisible && !IsErrorVisible && !IsDebugVisible;

    public DateTime FromTime
    {
        get => _fromTime;
        set
        {
            var delta = new TimeSpan(
                _toTime.TimeOfDay.Hours - _fromTime.TimeOfDay.Hours,
                Math.Max(_toTime.TimeOfDay.Minutes - _fromTime.TimeOfDay.Minutes, 1),
                0
            );
            SetProperty(ref _fromTime, value);
            if (_fromTime.TimeOfDay.Hours > _toTime.TimeOfDay.Hours || _fromTime.TimeOfDay.Minutes > _toTime.TimeOfDay.Minutes)
            {
                ToTime = _fromTime + delta;
            }
        }
    }

    public DateTime ToTime
    {
        get => _toTime;
        set
        {
            var delta = new TimeSpan(
                _toTime.TimeOfDay.Hours - _fromTime.TimeOfDay.Hours,
                Math.Max(_toTime.TimeOfDay.Minutes - _fromTime.TimeOfDay.Minutes, 1),
                0
            );
            SetProperty(ref _toTime, value);
            if (_toTime.TimeOfDay.Hours < _fromTime.TimeOfDay.Hours || _toTime.TimeOfDay.Minutes < _fromTime.TimeOfDay.Minutes)
            {
                FromTime = _toTime - delta;
            }
        }
    }

    public void ResetFilter()
    {
        SearchMessage = null;
        IsInfoVisible = false;
        IsWarnVisible = false;
        IsErrorVisible = false;
        FromTime = DateTime.Today;
        ToTime = DateTime.Today + new TimeSpan(23, 59, 59);
    }
}
