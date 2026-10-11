using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SetSupply.Helper;
using SetSupply.Models.Database;
using SetSupply.Service;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SetSupply.ViewModels.UserControlViewModels.Setting.Sub;

/// <summary>
/// Cài đặt nhóm model + sản lượng kế hoạch theo ca (hiển thị ở màn hình Home).
/// </summary>
public partial class UCModelGroupSettingViewModel : ObservableObject
{
    private readonly ModelGroupService _modelGroupService;

    public ObservableCollection<ModelGroup> Groups { get; } = new();

    [ObservableProperty]
    private ModelGroup? selectedGroup;

    [ObservableProperty]
    private int autoPrefixLength = 8;

    public string ImageFolder => ModelImageProvider.ImageFolder;

    public UCModelGroupSettingViewModel(ModelGroupService modelGroupService)
    {
        _modelGroupService = modelGroupService;
    }

    [RelayCommand]
    public async Task Load()
    {
        try
        {
            var groups = await _modelGroupService.GetAllAsync();
            Groups.Clear();
            foreach (var g in groups)
                Groups.Add(g);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải nhóm model: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Add()
    {
        var group = new ModelGroup
        {
            Name = "",
            SortOrder = Groups.Where(g => !g.IsCatchAll).Select(g => g.SortOrder).DefaultIfEmpty(0).Max() + 1
        };
        Groups.Add(group);
        SelectedGroup = group;
    }

    [RelayCommand]
    private void Delete()
    {
        if (SelectedGroup != null)
            Groups.Remove(SelectedGroup);
    }

    /// <summary>
    /// Tạo nhóm từ các model đã sản xuất 30 ngày gần nhất, theo N ký tự đầu (VD: NR-C33ES2L-C => NR-C33ES).
    /// </summary>
    [RelayCommand]
    private async Task AutoCreate()
    {
        try
        {
            int length = Math.Max(1, AutoPrefixLength);
            var models = await _modelGroupService.GetRecentModelsAsync(30);
            var existing = Groups.ToList();
            int order = existing.Where(g => !g.IsCatchAll).Select(g => g.SortOrder).DefaultIfEmpty(0).Max();
            int added = 0;

            foreach (var prefix in models.Select(m => m.Length > length ? m.Substring(0, length) : m).Distinct())
            {
                var matched = ModelGroupService.Match(prefix, existing);
                if (matched != null && !matched.IsCatchAll) continue;

                var group = new ModelGroup { Name = prefix, Prefixes = prefix, SortOrder = ++order };
                Groups.Add(group);
                existing.Add(group);
                added++;
            }

            MessageBox.Show(added > 0 ? $"Đã thêm {added} nhóm. Nhập kế hoạch rồi bấm Lưu." : "Không có nhóm mới.",
                "Tạo nhóm", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        foreach (var g in Groups)
        {
            g.Name = g.Name?.Trim() ?? "";
            // Chưa nhập tiền tố => dùng tên nhóm (trừ nhóm "Khác")
            if (string.IsNullOrWhiteSpace(g.Prefixes) &&
                !string.Equals(g.Name, ModelGroupService.OtherGroupName, StringComparison.OrdinalIgnoreCase))
                g.Prefixes = g.Name;
        }

        if (Groups.Any(g => string.IsNullOrEmpty(g.Name)))
        {
            MessageBox.Show("Tên nhóm không được để trống.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (Groups.GroupBy(g => g.Name, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1))
        {
            MessageBox.Show("Tên nhóm bị trùng.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (Groups.Count(g => g.IsCatchAll) > 1)
        {
            MessageBox.Show("Chỉ được có 1 nhóm \"Khác\" (tiền tố để trống).", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (Groups.Any(g => g.PlanShift1 < 0 || g.PlanShift2 < 0 || g.PlanShift3 < 0))
        {
            MessageBox.Show("Kế hoạch không được âm.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await _modelGroupService.SaveAllAsync(Groups);
            await Load();
            MessageBox.Show("Đã lưu cài đặt nhóm model.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi lưu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
