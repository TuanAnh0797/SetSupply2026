using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SetSupply.Helper;
using SetSupply.Models.Database;
using SetSupply.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SetSupply.ViewModels.UserControlViewModels.Setting.Sub;

/// <summary>
/// Cài đặt nhóm model (gom các model cùng nhóm để hiển thị ở màn hình Home). Có Import / Export Excel, CSV.
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

    // ===== IMPORT / EXPORT =====

    private static readonly string[] FileHeaders = { "Thứ tự", "Tên nhóm", "Tiền tố model" };

    /// <summary>
    /// Xuất danh sách đang hiển thị (kể cả phần chưa lưu) ra Excel / CSV. Có thể dùng làm file mẫu để Import.
    /// </summary>
    [RelayCommand]
    private void Export()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = SpreadsheetHelper.FileFilter,
            FileName = $"ModelGroup_{DateTime.Now:yyyyMMdd}.xlsx",
            Title = "Xuất nhóm model"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var rows = Groups.Select(g => new object?[] { g.SortOrder, g.Name, g.Prefixes });
            SpreadsheetHelper.Write(dialog.FileName, "ModelGroup", FileHeaders, rows);
            MessageBox.Show($"Đã xuất {Groups.Count} nhóm ra file:\n{dialog.FileName}", "Xuất file", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi xuất file: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Nhập danh sách nhóm từ file (cột: Thứ tự | Tên nhóm | Tiền tố model) => thay danh sách trên màn hình, bấm Lưu để áp dụng.
    /// </summary>
    [RelayCommand]
    private void Import()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = SpreadsheetHelper.FileFilter, Title = "Nhập nhóm model" };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var rows = SpreadsheetHelper.Read(dialog.FileName, FileHeaders.Length);
            var imported = new List<ModelGroup>();
            var errors = new List<string>();

            foreach (var (rowNumber, cells) in rows)
            {
                string name = cells[1].Trim();
                if (name.Length == 0)
                {
                    errors.Add($"Dòng {rowNumber}: thiếu tên nhóm");
                    continue;
                }

                int sortOrder = imported.Count + 1;
                if (cells[0].Trim().Length > 0 && !int.TryParse(cells[0].Trim(), out sortOrder))
                {
                    errors.Add($"Dòng {rowNumber}: thứ tự '{cells[0]}' không phải số");
                    continue;
                }

                if (imported.Any(g => string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase)))
                {
                    errors.Add($"Dòng {rowNumber}: tên nhóm '{name}' bị trùng");
                    continue;
                }

                imported.Add(new ModelGroup { SortOrder = sortOrder, Name = name, Prefixes = cells[2].Trim() });
            }

            if (errors.Count > 0)
            {
                MessageBox.Show("File có lỗi, chưa nhập:\n" + string.Join("\n", errors.Take(15)) +
                                (errors.Count > 15 ? $"\n... và {errors.Count - 15} lỗi khác" : ""),
                    "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (imported.Count == 0)
            {
                MessageBox.Show("File không có dữ liệu.", "Nhập file", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (Groups.Count > 0 && MessageBox.Show(
                    $"Thay toàn bộ {Groups.Count} nhóm đang có bằng {imported.Count} nhóm trong file?",
                    "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            Groups.Clear();
            foreach (var group in imported.OrderBy(g => g.SortOrder))
                Groups.Add(group);

            MessageBox.Show($"Đã nhập {imported.Count} nhóm. Kiểm tra lại rồi bấm Lưu để áp dụng.", "Nhập file",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (IOException ex)
        {
            MessageBox.Show($"Không đọc được file (file đang mở trong Excel?): {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi nhập file: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ===== ẢNH MODEL =====

    private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".bmp" };
    private const string ImageFilter = "Ảnh (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp";

    /// <summary>
    /// Chọn ảnh cho nhóm đang chọn => lưu thành &lt;tên nhóm&gt;.ext, dùng cho các model chưa có ảnh riêng.
    /// </summary>
    [RelayCommand]
    private void ChooseGroupImage()
    {
        if (SelectedGroup == null || string.IsNullOrWhiteSpace(SelectedGroup.Name))
        {
            MessageBox.Show("Chọn 1 nhóm (đã có tên) trong bảng trước.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = ImageFilter, Title = $"Chọn ảnh cho nhóm {SelectedGroup.Name}" };
        if (dialog.ShowDialog() != true) return;

        try
        {
            Directory.CreateDirectory(ImageFolder);
            var safeName = string.Concat(SelectedGroup.Name.Trim().Split(Path.GetInvalidFileNameChars()));

            // Bỏ ảnh cũ của nhóm (có thể khác đuôi file)
            foreach (var ext in ImageExtensions)
            {
                var old = Path.Combine(ImageFolder, safeName + ext);
                if (File.Exists(old)) File.Delete(old);
            }

            File.Copy(dialog.FileName, Path.Combine(ImageFolder, safeName + Path.GetExtension(dialog.FileName).ToLowerInvariant()), true);
            ModelImageProvider.ClearCache();
            MessageBox.Show($"Đã đặt ảnh cho nhóm {SelectedGroup.Name}.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi chép ảnh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Thêm ảnh riêng cho từng model: chọn nhiều file, tên file phải là tên model (VD NR-C33ES2-C.png).
    /// </summary>
    [RelayCommand]
    private void AddModelImages()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = ImageFilter,
            Multiselect = true,
            Title = "Chọn ảnh model (tên file = tên model, VD NR-C33ES2-C.png)"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            Directory.CreateDirectory(ImageFolder);
            foreach (var file in dialog.FileNames)
                File.Copy(file, Path.Combine(ImageFolder, Path.GetFileName(file)), true);
            ModelImageProvider.ClearCache();
            MessageBox.Show($"Đã thêm {dialog.FileNames.Length} ảnh.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi chép ảnh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void OpenImageFolder()
    {
        try
        {
            Directory.CreateDirectory(ImageFolder);
            Process.Start("explorer.exe", ImageFolder);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không mở được thư mục: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
