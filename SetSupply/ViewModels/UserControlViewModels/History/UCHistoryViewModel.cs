using APP.Database;
using APP.ViewModels.UserControlViewModels.Home;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SetSupply.Models.Database;
using SetSupply.Models.Home;
using SetSupply.Service;
using SetSupply.ViewModels.UserControlViewModels.Home;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SetSupply.ViewModels.UserControlViewModels.History;

public partial class UCHistoryViewModel : ObservableObject
{
    private readonly AppDbContext _db;
    private readonly ShiftSettingService _shiftService;
    private readonly ProductionDataService _productionService;
    private bool _isInitialized;

    // 3 ca làm việc (reuse ShiftViewModel từ Home)
    public ShiftViewModel Shift1 { get; }
    public ShiftViewModel Shift2 { get; }
    public ShiftViewModel Shift3 { get; }

    private readonly ShiftViewModel[] _allShifts;

    // ===== PROPERTIES =====

    [ObservableProperty]
    private int selectedTabIndex;

    [ObservableProperty]
    private DateTime selectedDate = DateTime.Today.AddDays(-1); // Mặc định hôm qua

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusText = "Chọn ngày để xem lịch sử sản lượng";

    // Tổng sản lượng cả ngày (tất cả ca)
    [ObservableProperty]
    private int dailyTotalOutput;

    // Lịch sử cài đặt ca cho ngày đã chọn
    [ObservableProperty]
    private ObservableCollection<ShiftSettingHistoryDisplay> shiftSettingHistories;

    [ObservableProperty]
    private bool hasShiftSettingHistory;

    public UCHistoryViewModel(AppDbContext db, ShiftSettingService shiftService, ProductionDataService productionService)
    {
        _db = db;
        _shiftService = shiftService;
        _productionService = productionService;

        Shift1 = new ShiftViewModel(1, "CA 1");
        Shift2 = new ShiftViewModel(2, "CA 2");
        Shift3 = new ShiftViewModel(3, "CA 3");

        _allShifts = new[] { Shift1, Shift2, Shift3 };

        ShiftSettingHistories = new ObservableCollection<ShiftSettingHistoryDisplay>();
    }

    /// <summary>
    /// Khởi tạo - gọi từ code-behind khi Loaded
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await LoadDataForDateAsync();
        _isInitialized = true;
    }

    // Event báo cho View biết data đã load xong
    public event Action DataLoaded;

    /// <summary>
    /// Khi SelectedDate thay đổi -> tự động reload
    /// </summary>
    partial void OnSelectedDateChanged(DateTime value)
    {
        _ = LoadDataForDateAsync();
    }

    /// <summary>
    /// Load tất cả dữ liệu cho ngày đã chọn
    /// </summary>
    private async Task LoadDataForDateAsync()
    {
        IsLoading = true;
        StatusText = $"Đang tải dữ liệu ngày {SelectedDate:dd/MM/yyyy}...";

        try
        {
            // 1. Load cài đặt ca cho ngày đó (từ history hoặc current)
            await LoadShiftSettingsForDateAsync();

            // 2. Load dữ liệu sản lượng
            await LoadProductionDataAsync();

            // 3. Load lịch sử thay đổi cài đặt ca
            await LoadShiftSettingHistoryAsync();

            // 4. Cập nhật tổng ngày
            DailyTotalOutput = Shift1.TotalOutput + Shift2.TotalOutput + Shift3.TotalOutput;

            bool hasData = DailyTotalOutput > 0;
            StatusText = hasData
                ? $"Ngày {SelectedDate:dd/MM/yyyy} - Tổng sản lượng: {DailyTotalOutput}"
                : $"Ngày {SelectedDate:dd/MM/yyyy} - Không có dữ liệu";

            // 5. Báo cho View regenerate columns
            DataLoaded?.Invoke();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading history data: {ex.Message}");
            StatusText = $"Lỗi tải dữ liệu: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Load cài đặt ca: ưu tiên lịch sử, fallback về cài đặt hiện tại
    /// </summary>
    private async Task LoadShiftSettingsForDateAsync()
    {
        try
        {
            // Thử lấy từ lịch sử trước
            var history = await _shiftService.GetHistoryByDateAsync(SelectedDate);

            if (history != null && history.Count >= 3)
            {
                // Dùng cài đặt lịch sử
                var grouped = history
                    .GroupBy(h => h.ShiftNumber)
                    .OrderBy(g => g.Key)
                    .Select(g => g.OrderByDescending(h => h.InsertTime).First())
                    .ToList();

                if (grouped.Count >= 3)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        var s = grouped[i];
                        _allShifts[i].ApplySettings(new ShiftSettings
                        {
                            ShiftNumber = s.ShiftNumber,
                            StartHour = s.StartTime.Hours,
                            EndHour = s.EndTime.Hours,
                            StartTime = s.StartTime,
                            EndTime = s.EndTime,
                            TargetPerHour = 100
                        });
                    }
                    return;
                }
            }

            // Fallback: dùng cài đặt hiện tại
            var currentSettings = await _shiftService.GetAllShiftSettingsAsync();

            if (currentSettings != null && currentSettings.Count >= 3)
            {
                for (int i = 0; i < 3; i++)
                {
                    _allShifts[i].ApplySettings(new ShiftSettings
                    {
                        ShiftNumber = currentSettings[i].ShiftNumber,
                        StartHour = currentSettings[i].StartTime.Hours,
                        EndHour = currentSettings[i].EndTime.Hours,
                        StartTime = currentSettings[i].StartTime,
                        EndTime = currentSettings[i].EndTime,
                        TargetPerHour = 100
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
            System.Diagnostics.Debug.WriteLine($"Error loading shift settings for history: {ex.Message}");
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

    /// <summary>
    /// Load dữ liệu sản lượng cho ngày đã chọn
    /// </summary>
    private async Task LoadProductionDataAsync()
    {
        try
        {
            foreach (var shift in _allShifts)
            {
                if (shift.Settings == null) continue;

                var matrix = await _productionService.GetProductionMatrixAsync(SelectedDate, shift.ShiftNumber);

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

    /// <summary>
    /// Load lịch sử thay đổi cài đặt ca cho ngày đã chọn
    /// </summary>
    private async Task LoadShiftSettingHistoryAsync()
    {
        try
        {
            var histories = await _shiftService.GetHistoryByDateAsync(SelectedDate);

            Application.Current.Dispatcher.Invoke(() =>
            {
                ShiftSettingHistories.Clear();

                if (histories != null && histories.Count > 0)
                {
                    foreach (var h in histories.OrderBy(x => x.ShiftNumber).ThenByDescending(x => x.InsertTime))
                    {
                        ShiftSettingHistories.Add(new ShiftSettingHistoryDisplay
                        {
                            ShiftNumber = h.ShiftNumber,
                            ShiftName = $"CA {h.ShiftNumber}",
                            TimeRange = $"{h.StartTime:hh\\:mm} - {h.EndTime:hh\\:mm}",
                            IsUse = h.IsUse,
                            IsUseText = h.IsUse ? "Sử dụng" : "Không sử dụng",
                            ChangedAt = h.InsertTime.ToString("dd/MM/yyyy HH:mm:ss")
                        });
                    }
                }

                HasShiftSettingHistory = ShiftSettingHistories.Count > 0;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading shift setting history: {ex.Message}");
        }
    }

    // ===== COMMANDS =====

    [RelayCommand]
    private async Task RefreshData()
    {
        _isInitialized = false;
        await LoadDataForDateAsync();
    }

    [RelayCommand]
    private void GoToPreviousDay()
    {
        SelectedDate = SelectedDate.AddDays(-1);
    }

    [RelayCommand]
    private void GoToNextDay()
    {
        if (SelectedDate.Date < DateTime.Today)
            SelectedDate = SelectedDate.AddDays(1);
    }

    [RelayCommand]
    private void GoToToday()
    {
        SelectedDate = DateTime.Today;
    }
}

/// <summary>
/// Model hiển thị lịch sử thay đổi cài đặt ca
/// </summary>
public class ShiftSettingHistoryDisplay
{
    public int ShiftNumber { get; set; }
    public string ShiftName { get; set; }
    public string TimeRange { get; set; }
    public bool IsUse { get; set; }
    public string IsUseText { get; set; }
    public string ChangedAt { get; set; }
}
