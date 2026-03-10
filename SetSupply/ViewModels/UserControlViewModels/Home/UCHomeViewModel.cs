using APP.Database;
using APP.Models.Home;
using APP.Service;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SetSupply.Models.Database;
using SetSupply.Models.Home;
using SetSupply.Service;
using SetSupply.ViewModels.UserControlViewModels.Home;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace APP.ViewModels.UserControlViewModels.Home;

public partial class UCHomeViewModel : ObservableObject
{
    private readonly AppDbContext _db;
    private readonly ShiftSettingService _shiftService;
    private readonly ProductionDataService _productionService;

    private readonly PLCService _plcService;

    //private DispatcherTimer _timer;
    private bool _isInitialized;

    // 3 ca làm việc
    public ShiftViewModel Shift1 { get; }
    public ShiftViewModel Shift2 { get; }
    public ShiftViewModel Shift3 { get; }

    // Mảng để duyệt vòng lặp
    private readonly ShiftViewModel[] _allShifts;

    [ObservableProperty]
    private DateTime currentTime;

    [ObservableProperty]
    private string currentShiftName;

    [ObservableProperty]
    private int selectedTabIndex;

    [ObservableProperty]
    private DateTime selectedDate = DateTime.Today;

    [ObservableProperty]
    private bool autoSwitchTab = true;

    [ObservableProperty]
    private string autoSwitchButtonText = "Tắt Tự Động";

    public UCHomeViewModel(AppDbContext db, ShiftSettingService shiftService, ProductionDataService productionService, PLCService pLCService)
    {
        _db = db;
        _shiftService = shiftService;
        _productionService = productionService;
        _plcService = pLCService;

        Shift1 = new ShiftViewModel(1, "CA 1");
        Shift2 = new ShiftViewModel(2, "CA 2");
        Shift3 = new ShiftViewModel(3, "CA 3");

        _allShifts = new[] { Shift1, Shift2, Shift3 };

        _plcService.DataReceived += _plcService_DataReceived;

    }

    private async void _plcService_DataReceived(string data)
    {

        if (data.Length >= 19)
        {
            string codeback = data;
            string model = codeback.Substring(0, 12);
            string serial = codeback.Substring(12, 7);
            await _productionService.AddProductionDataAsync(codeback, data, serial);
            CurrentTime = DateTime.Now;
            UpdateCurrentShift();
            await LoadProductionDataAsync();
        }
    }

    /// <summary>
    /// Khởi tạo async - gọi từ code-behind khi Loaded
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await LoadShiftSettingsAsync();
        await LoadProductionDataAsync();

        _isInitialized = true;
    }

    [RelayCommand]
    private void ToggleAutoSwitch()
    {
        AutoSwitchTab = !AutoSwitchTab;
        AutoSwitchButtonText = AutoSwitchTab ? "Tắt Tự Động" : "Bật Tự Động";
    }

    private async Task LoadShiftSettingsAsync()
    {
        try
        {
            var settings = await _shiftService.GetAllShiftSettingsAsync();

            if (settings != null && settings.Count >= 3)
            {
                for (int i = 0; i < 3; i++)
                {
                    _allShifts[i].ApplySettings(new ShiftSettings
                    {
                        ShiftNumber = settings[i].ShiftNumber,
                        StartHour = settings[i].StartTime.Hours,
                        EndHour = settings[i].EndTime.Hours,
                        StartTime = settings[i].StartTime,
                        EndTime = settings[i].EndTime,
                    });
                }
            }
            else
            {
                ApplyDefaultSettings();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi tải cài đặt ca: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            ApplyDefaultSettings();
        }
    }

    private void ApplyDefaultSettings()
    {
        var defaults = new (int start, int end)[] { (6, 14), (14, 22), (22, 6) };

        for (int i = 0; i < 3; i++)
        {
            _allShifts[i].ApplySettings(new ShiftSettings
            {
                ShiftNumber = i + 1,
                StartHour = defaults[i].start,
                EndHour = defaults[i].end,
                StartTime = new TimeSpan(defaults[i].start, 0, 0),
                EndTime = new TimeSpan(defaults[i].end, 0, 0),
                TargetPerHour = 100
            });
        }
    }

    private async Task LoadProductionDataAsync()
    {
        try
        {
            foreach (var shift in _allShifts)
            {
                if (shift.Settings == null) continue;

                var matrix = await _productionService.GetProductionMatrixAsync(CurrentTime, shift.ShiftNumber);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    shift.UpdateProductionData(matrix);
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading production data: {ex.Message}");
        }
    }





    private void UpdateCurrentShift()
    {
        var now = CurrentTime.TimeOfDay;

        for (int i = 0; i < _allShifts.Length; i++)
        {
            var shift = _allShifts[i];
            if (shift.Settings == null) continue;

            if (IsInShift(now, shift.Settings.StartTime, shift.Settings.EndTime))
            {
                CurrentShiftName = $"{shift.ShiftName} - {shift.TimeRange}";
                if (AutoSwitchTab)
                    SelectedTabIndex = i;
                return;
            }
        }
    }

    private static bool IsInShift(TimeSpan currentTime, TimeSpan startTime, TimeSpan endTime)
    {
        if (endTime > startTime)
            return currentTime >= startTime && currentTime < endTime;
        else
            return currentTime >= startTime || currentTime < endTime;
    }

    [RelayCommand]
    private async Task RefreshData()
    {


        try
        {
            await LoadShiftSettingsAsync();
            await LoadProductionDataAsync();
            await _productionService.ReloadShiftSettingsAsync();
        }
        catch (Exception ex)
        {
           


        }
    }
    [RelayCommand]
    private async Task ChangeDate(DateTime date)
    {


        try
        {
            SelectedDate = date;
            await LoadProductionDataAsync();
        }
        catch (Exception ex)
        {
            { }

        }


    } 
}
