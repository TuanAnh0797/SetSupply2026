using APP.Database;
using SetSupply.Service;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace SetSupply.ViewModels.UserControlViewModels.Setting.Sub;

public partial class UCShiftSettingViewModel : ObservableObject
{
    private readonly AppDbContext _db;
    private readonly ShiftSettingService _shiftService;

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

    public UCShiftSettingViewModel(AppDbContext db, ShiftSettingService shiftService)
    {
        _db = db;
        _shiftService = shiftService;

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
    }

    private void UpdateShift2TimeRange()
    {
        Shift2TimeRange = $"{Shift2StartHour:D2}:{Shift2StartMinute:D2} - {Shift2EndHour:D2}:{Shift2EndMinute:D2}";
    }

    private void UpdateShift3TimeRange()
    {
        Shift3TimeRange = $"{Shift3StartHour:D2}:{Shift3StartMinute:D2} - {Shift3EndHour:D2}:{Shift3EndMinute:D2}";
    }

    private async void LoadSettings()
    {
        try
        {
            var settings = await _shiftService.GetAllShiftSettingsAsync();

            if (settings != null && settings.Count >= 3)
            {
                // CA 1
                Shift1StartHour = settings[0].StartTime.Hours;
                Shift1StartMinute = settings[0].StartTime.Minutes;
                Shift1EndHour = settings[0].EndTime.Hours;
                Shift1EndMinute = settings[0].EndTime.Minutes;
                Shift1IsUse = settings[0].IsUse;

                // CA 2
                Shift2StartHour = settings[1].StartTime.Hours;
                Shift2StartMinute = settings[1].StartTime.Minutes;
                Shift2EndHour = settings[1].EndTime.Hours;
                Shift2EndMinute = settings[1].EndTime.Minutes;
                Shift2IsUse = settings[1].IsUse;

                // CA 3
                Shift3StartHour = settings[2].StartTime.Hours;
                Shift3StartMinute = settings[2].StartTime.Minutes;
                Shift3EndHour = settings[2].EndTime.Hours;
                Shift3EndMinute = settings[2].EndTime.Minutes;
                Shift3IsUse = settings[2].IsUse;

                UpdateShift1TimeRange();
                UpdateShift2TimeRange();
                UpdateShift3TimeRange();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải cài đặt: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        try
        {
            // Lưu CA 1
            await _shiftService.UpdateShiftSettingAsync(
                1,
                new TimeSpan(Shift1StartHour, Shift1StartMinute, 0),
                new TimeSpan(Shift1EndHour, Shift1EndMinute, 0),
                Shift1IsUse,
                DateTime.Today
            );

            // Lưu CA 2
            await _shiftService.UpdateShiftSettingAsync(
                2,
                new TimeSpan(Shift2StartHour, Shift2StartMinute, 0),
                new TimeSpan(Shift2EndHour, Shift2EndMinute, 0),
                Shift2IsUse,
                DateTime.Today
            );

            // Lưu CA 3
            await _shiftService.UpdateShiftSettingAsync(
                3,
                new TimeSpan(Shift3StartHour, Shift3StartMinute, 0),
                new TimeSpan(Shift3EndHour, Shift3EndMinute, 0),
                Shift3IsUse,
                DateTime.Today
            );

            MessageBox.Show("Lưu cài đặt thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi lưu cài đặt: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task Reload()
    {
        LoadSettings();
    }
}