using APP.Database;
using SetSupply.Service;
using SetSupply.Models.Home;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SetSupply.ViewModels.UserControlViewModels.Setting.Sub;

/// <summary>
/// 1 dòng giờ nghỉ trên trang cài đặt ca (nhập giờ dạng HH:mm).
/// </summary>
public partial class BreakItem : ObservableObject
{
    public BreakItem(int shiftNumber) => ShiftNumber = shiftNumber;

    public int ShiftNumber { get; }

    [ObservableProperty] private string startText = string.Empty;
    [ObservableProperty] private string endText = string.Empty;
    [ObservableProperty] private string name = string.Empty;

    public TimeSpan? Start => ShiftScheduleService.TryParseClock(StartText);
    public TimeSpan? End => ShiftScheduleService.TryParseClock(EndText);
    public bool IsValid => Start.HasValue && End.HasValue && Start != End;
}

public partial class UCShiftSettingViewModel : ObservableObject
{
    private readonly AppDbContext _db;
    private readonly ShiftSettingService _shiftService;
    private readonly ShiftScheduleService _scheduleService;
    private bool _loading;

    // CA 1
    [ObservableProperty]
    private int shift1StartHour = 6;

    [ObservableProperty]
    private int shift1StartMinute = 0;

    [ObservableProperty]
    private int shift1EndHour = 14;

    [ObservableProperty]
    private int shift1EndMinute = 0;

    [ObservableProperty]
    private bool shift1IsUse = true;

    [ObservableProperty]
    private string shift1TimeRange = "";

    // CA 2
    [ObservableProperty]
    private int shift2StartHour = 14;

    [ObservableProperty]
    private int shift2StartMinute = 0;

    [ObservableProperty]
    private int shift2EndHour = 22;

    [ObservableProperty]
    private int shift2EndMinute = 0;

    [ObservableProperty]
    private bool shift2IsUse = true;

    [ObservableProperty]
    private string shift2TimeRange = "";

    // CA 3
    [ObservableProperty]
    private int shift3StartHour = 22;

    [ObservableProperty]
    private int shift3StartMinute = 0;

    [ObservableProperty]
    private int shift3EndHour = 6;

    [ObservableProperty]
    private int shift3EndMinute = 0;

    [ObservableProperty]
    private bool shift3IsUse = true;

    [ObservableProperty]
    private string shift3TimeRange = "";

    // Giờ nghỉ từng ca
    public ObservableCollection<BreakItem> Shift1Breaks { get; } = new();
    public ObservableCollection<BreakItem> Shift2Breaks { get; } = new();
    public ObservableCollection<BreakItem> Shift3Breaks { get; } = new();

    // "Làm việc 7h20' • nghỉ 40'"
    [ObservableProperty] private string shift1WorkText = "";
    [ObservableProperty] private string shift2WorkText = "";
    [ObservableProperty] private string shift3WorkText = "";

    public UCShiftSettingViewModel(AppDbContext db, ShiftSettingService shiftService, ShiftScheduleService scheduleService)
    {
        _db = db;
        _shiftService = shiftService;
        _scheduleService = scheduleService;

        foreach (var list in new[] { Shift1Breaks, Shift2Breaks, Shift3Breaks })
            list.CollectionChanged += (_, _) => UpdateWorkTexts();

        LoadSettings();
    }

    // Auto update TimeRange khi thay đổi giờ
    partial void OnShift1StartHourChanged(int value) => UpdateShift1TimeRange();
    partial void OnShift1StartMinuteChanged(int value) => UpdateShift1TimeRange();
    partial void OnShift1EndHourChanged(int value) => UpdateShift1TimeRange();
    partial void OnShift1EndMinuteChanged(int value) => UpdateShift1TimeRange();

    partial void OnShift2StartHourChanged(int value) => UpdateShift2TimeRange();
    partial void OnShift2StartMinuteChanged(int value) => UpdateShift2TimeRange();
    partial void OnShift2EndHourChanged(int value) => UpdateShift2TimeRange();
    partial void OnShift2EndMinuteChanged(int value) => UpdateShift2TimeRange();

    partial void OnShift3StartHourChanged(int value) => UpdateShift3TimeRange();
    partial void OnShift3StartMinuteChanged(int value) => UpdateShift3TimeRange();
    partial void OnShift3EndHourChanged(int value) => UpdateShift3TimeRange();
    partial void OnShift3EndMinuteChanged(int value) => UpdateShift3TimeRange();

    private void UpdateShift1TimeRange()
    {
        Shift1TimeRange = $"{Shift1StartHour:D2}:{Shift1StartMinute:D2} - {Shift1EndHour:D2}:{Shift1EndMinute:D2}";
        UpdateWorkTexts();
    }

    private void UpdateShift2TimeRange()
    {
        Shift2TimeRange = $"{Shift2StartHour:D2}:{Shift2StartMinute:D2} - {Shift2EndHour:D2}:{Shift2EndMinute:D2}";
        UpdateWorkTexts();
    }

    private void UpdateShift3TimeRange()
    {
        Shift3TimeRange = $"{Shift3StartHour:D2}:{Shift3StartMinute:D2} - {Shift3EndHour:D2}:{Shift3EndMinute:D2}";
        UpdateWorkTexts();
    }

    private async void LoadSettings()
    {
        try
        {
            _loading = true;
            var settings = await _scheduleService.GetCurrentAsync();

            if (settings != null && settings.Count >= 3)
            {
                // CA 1
                Shift1StartHour = settings[0].Start.Hours;
                Shift1StartMinute = settings[0].Start.Minutes;
                Shift1EndHour = settings[0].End.Hours;
                Shift1EndMinute = settings[0].End.Minutes;
                Shift1IsUse = settings[0].IsUse;

                // CA 2
                Shift2StartHour = settings[1].Start.Hours;
                Shift2StartMinute = settings[1].Start.Minutes;
                Shift2EndHour = settings[1].End.Hours;
                Shift2EndMinute = settings[1].End.Minutes;
                Shift2IsUse = settings[1].IsUse;

                // CA 3
                Shift3StartHour = settings[2].Start.Hours;
                Shift3StartMinute = settings[2].Start.Minutes;
                Shift3EndHour = settings[2].End.Hours;
                Shift3EndMinute = settings[2].End.Minutes;
                Shift3IsUse = settings[2].IsUse;

                FillBreaks(Shift1Breaks, settings[0]);
                FillBreaks(Shift2Breaks, settings[1]);
                FillBreaks(Shift3Breaks, settings[2]);

                UpdateShift1TimeRange();
                UpdateShift2TimeRange();
                UpdateShift3TimeRange();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải cài đặt: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _loading = false;
            UpdateWorkTexts();
        }
    }

    private void FillBreaks(ObservableCollection<BreakItem> list, ShiftDefinition shift)
    {
        list.Clear();
        foreach (var b in shift.Breaks)
            list.Add(CreateBreak(shift.ShiftNumber, $"{b.Start:hh\\:mm}", $"{b.End:hh\\:mm}", b.Name));
    }

    private BreakItem CreateBreak(int shiftNumber, string start, string end, string name)
    {
        var item = new BreakItem(shiftNumber) { StartText = start, EndText = end, Name = name };
        item.PropertyChanged += (_, _) => UpdateWorkTexts();
        return item;
    }

    private ObservableCollection<BreakItem> BreaksOf(int shiftNumber) => shiftNumber switch
    {
        1 => Shift1Breaks,
        2 => Shift2Breaks,
        _ => Shift3Breaks
    };

    /// <summary>Thêm 1 dòng giờ nghỉ (gợi ý: sau giờ nghỉ cuối hoặc giữa ca).</summary>
    [RelayCommand]
    private void AddBreak(object? parameter)
    {
        int shiftNumber = Convert.ToInt32(parameter);
        var shift = BuildShifts().First(s => s.ShiftNumber == shiftNumber);
        var list = BreaksOf(shiftNumber);

        var suggestStart = list.Count == 0
            ? shift.Start + TimeSpan.FromMinutes(Math.Round(ShiftLengthMinutes(shift) / 2 / 30) * 30)
            : (list[^1].End ?? shift.Start) + TimeSpan.FromHours(2);
        suggestStart = TimeSpan.FromMinutes(((int)suggestStart.TotalMinutes % 1440 + 1440) % 1440);
        var suggestEnd = TimeSpan.FromMinutes(((int)suggestStart.TotalMinutes + 30) % 1440);

        list.Add(CreateBreak(shiftNumber, $"{suggestStart:hh\\:mm}", $"{suggestEnd:hh\\:mm}", list.Count == 0 ? "Nghỉ" : string.Empty));
    }

    [RelayCommand]
    private void RemoveBreak(BreakItem? item)
    {
        if (item == null) return;
        BreaksOf(item.ShiftNumber).Remove(item);
    }

    private static double ShiftLengthMinutes(ShiftDefinition shift)
    {
        var (s, e) = shift.GetRange(DateTime.Today);
        return (e - s).TotalMinutes;
    }

    /// <summary>Cài đặt đang nhập trên màn hình (giờ nghỉ sai định dạng bị bỏ qua).</summary>
    private List<ShiftDefinition> BuildShifts()
    {
        ShiftDefinition Make(int n, int sh, int sm, int eh, int em, bool use, IEnumerable<BreakItem> breaks) => new()
        {
            ShiftNumber = n,
            Start = new TimeSpan(sh, sm, 0),
            End = new TimeSpan(eh, em, 0),
            IsUse = use,
            Breaks = breaks.Where(b => b.IsValid)
                           .Select(b => new BreakDefinition { Start = b.Start!.Value, End = b.End!.Value, Name = b.Name?.Trim() ?? string.Empty })
                           .ToList()
        };

        return new List<ShiftDefinition>
        {
            Make(1, Shift1StartHour, Shift1StartMinute, Shift1EndHour, Shift1EndMinute, Shift1IsUse, Shift1Breaks),
            Make(2, Shift2StartHour, Shift2StartMinute, Shift2EndHour, Shift2EndMinute, Shift2IsUse, Shift2Breaks),
            Make(3, Shift3StartHour, Shift3StartMinute, Shift3EndHour, Shift3EndMinute, Shift3IsUse, Shift3Breaks),
        };
    }

    private void UpdateWorkTexts()
    {
        if (_loading) return;
        var shifts = BuildShifts();
        Shift1WorkText = WorkText(shifts[0], Shift1Breaks);
        Shift2WorkText = WorkText(shifts[1], Shift2Breaks);
        Shift3WorkText = WorkText(shifts[2], Shift3Breaks);
    }

    private static string WorkText(ShiftDefinition shift, IEnumerable<BreakItem> items)
    {
        if (items.Any(b => !b.IsValid))
            return "Có giờ nghỉ sai định dạng (nhập HH:mm, VD 10:00)";
        var day = DateTime.Today;
        double total = ShiftLengthMinutes(shift);
        double work = shift.TotalWorkMinutes(day);
        return $"Làm việc {FormatDuration(work)}  •  nghỉ {FormatDuration(total - work)}";
    }

    private static string FormatDuration(double minutes)
    {
        int m = (int)Math.Round(minutes);
        return m >= 60 ? $"{m / 60}h{(m % 60 > 0 ? $"{m % 60:D2}'" : "")}" : $"{m}'";
    }

    [RelayCommand]
    private async Task Save()
    {
        try
        {
            var invalid = Shift1Breaks.Concat(Shift2Breaks).Concat(Shift3Breaks).FirstOrDefault(b => !b.IsValid);
            if (invalid != null)
            {
                MessageBox.Show($"Ca {invalid.ShiftNumber}: giờ nghỉ \"{invalid.StartText} - {invalid.EndText}\" sai định dạng.\nNhập giờ dạng HH:mm, VD 10:00 - 10:10.",
                                "Chưa lưu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var shifts = BuildShifts();
            var error = ShiftScheduleService.Validate(shifts);
            if (error != null)
            {
                MessageBox.Show(error, "Chưa lưu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await _scheduleService.SaveAsync(shifts);

            MessageBox.Show("Lưu cài đặt thành công!\nÁp dụng từ ngày sản xuất hiện tại; các ngày đã qua giữ nguyên giờ ca / giờ nghỉ cũ.",
                            "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi lưu cài đặt: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private Task Reload()
    {
        LoadSettings();
        return Task.CompletedTask;
    }
}
