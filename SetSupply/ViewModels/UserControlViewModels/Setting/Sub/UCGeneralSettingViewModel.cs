using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SetSupply.Service;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace SetSupply.ViewModels.UserControlViewModels.Setting.Sub;

/// <summary>
/// Cài đặt chung: tên ứng dụng hiển thị trên thanh tiêu đề (để dùng chương trình cho line / vị trí khác).
/// </summary>
public partial class UCGeneralSettingViewModel : ObservableObject
{
    private readonly AppSettingService _settingService;

    [ObservableProperty]
    private string applicationTitle = AppSettingService.DefaultApplicationTitle;

    public string DefaultTitle => AppSettingService.DefaultApplicationTitle;

    public UCGeneralSettingViewModel(AppSettingService settingService)
    {
        _settingService = settingService;
    }

    [RelayCommand]
    public void Load()
    {
        ApplicationTitle = _settingService.ApplicationTitle;
    }

    [RelayCommand]
    private async Task Save()
    {
        var title = (ApplicationTitle ?? string.Empty).Trim();
        if (title.Length == 0)
        {
            MessageBox.Show("Tên ứng dụng không được để trống.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (title.Length > 60)
        {
            MessageBox.Show("Tên ứng dụng tối đa 60 ký tự.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await _settingService.SetAsync(AppSettingService.ApplicationTitleKey, title);
            ApplicationTitle = title;
            MessageBox.Show("Đã lưu tên ứng dụng.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi lưu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void RestoreDefault()
    {
        ApplicationTitle = AppSettingService.DefaultApplicationTitle;
    }
}
