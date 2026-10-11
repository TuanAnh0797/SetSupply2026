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
using System.Windows.Media;

namespace SetSupply.ViewModels.UserControlViewModels.Setting.Sub;

/// <summary>
/// 1 dòng gán model vào nhóm (có ảnh xem trước).
/// </summary>
public partial class ModelMappingRow : ObservableObject
{
    [ObservableProperty]
    private int sortOrder;

    [ObservableProperty]
    private string groupName = string.Empty;

    [ObservableProperty]
    private string modelName = string.Empty;

    [ObservableProperty]
    private string imagePath = string.Empty;

    [ObservableProperty]
    private ImageSource? preview;

    /// <summary>Có khai báo đường dẫn ảnh nhưng không tìm thấy file.</summary>
    [ObservableProperty]
    private bool imageMissing;

    partial void OnImagePathChanged(string value) => UpdatePreview();
    partial void OnModelNameChanged(string value) => UpdatePreview();
    partial void OnGroupNameChanged(string value) => UpdatePreview();

    public void UpdatePreview()
    {
        var resolved = ModelImageProvider.ResolvePath(ImagePath);
        ImageMissing = resolved != null && !File.Exists(resolved);
        Preview = ModelImageProvider.Get(ModelName ?? string.Empty, GroupName ?? string.Empty, ImagePath);
    }

    public ModelMapping ToEntity() => new()
    {
        SortOrder = SortOrder,
        GroupName = (GroupName ?? string.Empty).Trim(),
        ModelName = (ModelName ?? string.Empty).Trim(),
        ImagePath = (ImagePath ?? string.Empty).Trim()
    };

    public static ModelMappingRow From(ModelMapping m)
    {
        var row = new ModelMappingRow { SortOrder = m.SortOrder, GroupName = m.GroupName, ModelName = m.ModelName, ImagePath = m.ImagePath };
        row.UpdatePreview();
        return row;
    }
}

/// <summary>
/// Cài đặt nhóm model: gán TỪNG model vào nhóm (không dùng tiền tố) + ảnh model.
/// Model chưa gán => nhóm "Khác". Có Import / Export Excel, CSV (cột Nhóm model | Model | Đường dẫn ảnh).
/// </summary>
public partial class UCModelGroupSettingViewModel : ObservableObject
{
    private static readonly string[] FileHeaders = { "Nhóm model", "Model", "Đường dẫn ảnh" };
    private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".bmp" };
    private const string ImageFilter = "Ảnh (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp";

    private readonly ModelGroupService _modelGroupService;

    public ObservableCollection<ModelMappingRow> Rows { get; } = new();

    [ObservableProperty]
    private ModelMappingRow? selectedRow;

    [ObservableProperty]
    private string summaryText = string.Empty;

    public string ImageFolder => ModelImageProvider.ImageFolder;

    public UCModelGroupSettingViewModel(ModelGroupService modelGroupService)
    {
        _modelGroupService = modelGroupService;
        Rows.CollectionChanged += (_, _) => UpdateSummary();
    }

    [RelayCommand]
    public async Task Load()
    {
        try
        {
            ModelImageProvider.ClearCache();
            var mappings = await _modelGroupService.GetMappingsAsync();
            Rows.Clear();
            foreach (var m in mappings)
                Rows.Add(ModelMappingRow.From(m));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải nhóm model: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Add()
    {
        var row = new ModelMappingRow
        {
            SortOrder = Rows.Select(r => r.SortOrder).DefaultIfEmpty(0).Max() + 1,
            GroupName = SelectedRow?.GroupName ?? string.Empty // tiện thêm nhiều model cho cùng nhóm
        };
        Rows.Add(row);
        SelectedRow = row;
    }

    [RelayCommand]
    private void Delete()
    {
        if (SelectedRow != null)
            Rows.Remove(SelectedRow);
    }

    /// <summary>
    /// Thêm các model đã sản xuất 30 ngày gần nhất nhưng chưa được gán nhóm (cột nhóm để trống để điền).
    /// </summary>
    [RelayCommand]
    private async Task AddUnmappedModels()
    {
        try
        {
            var existing = new HashSet<string>(Rows.Select(r => r.ModelName.Trim()), StringComparer.OrdinalIgnoreCase);
            var models = (await _modelGroupService.GetRecentModelsAsync(30)).Where(m => !existing.Contains(m)).ToList();
            int order = Rows.Select(r => r.SortOrder).DefaultIfEmpty(0).Max();

            foreach (var model in models)
            {
                var row = new ModelMappingRow { SortOrder = ++order, ModelName = model };
                row.UpdatePreview();
                Rows.Add(row);
            }

            MessageBox.Show(models.Count > 0
                    ? $"Đã thêm {models.Count} model chưa có nhóm (cuối bảng). Điền cột Nhóm model rồi bấm Lưu."
                    : "Tất cả model đã sản xuất trong 30 ngày đều đã có nhóm.",
                "Model chưa có nhóm", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        var errors = Validate(Rows.Select((r, i) => (i + 1, r.GroupName, r.ModelName)));
        if (errors.Count > 0)
        {
            ShowErrors("Chưa lưu, dữ liệu có lỗi:", errors);
            return;
        }

        try
        {
            await _modelGroupService.SaveMappingsAsync(Rows.Select(r => r.ToEntity()));
            await Load();
            MessageBox.Show($"Đã lưu {Rows.Count} model.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi lưu: {ex.GetBaseException().Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ===== IMPORT / EXPORT =====

    /// <summary>
    /// Xuất bảng đang hiển thị (kể cả phần chưa lưu). Dùng làm file mẫu để Import.
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
            var rows = Rows.OrderBy(r => r.SortOrder).Select(r => new object?[] { r.GroupName, r.ModelName, r.ImagePath });
            SpreadsheetHelper.Write(dialog.FileName, "ModelGroup", FileHeaders, rows);
            MessageBox.Show($"Đã xuất {Rows.Count} model ra file:\n{dialog.FileName}", "Xuất file", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (IOException ex)
        {
            MessageBox.Show($"Không ghi được file (file đang mở trong Excel?): {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi xuất file: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Nhập từ file (cột: Nhóm model | Model | Đường dẫn ảnh) => thay bảng trên màn hình, bấm Lưu để áp dụng.
    /// Thứ tự hiển thị nhóm / model theo thứ tự dòng trong file.
    /// </summary>
    [RelayCommand]
    private void Import()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = SpreadsheetHelper.FileFilter, Title = "Nhập nhóm model" };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var rows = SpreadsheetHelper.Read(dialog.FileName, FileHeaders.Length);
            var errors = Validate(rows.Select(r => (r.RowNumber, r.Cells[0], r.Cells[1])));
            if (errors.Count > 0)
            {
                ShowErrors("File có lỗi, chưa nhập:", errors);
                return;
            }
            if (rows.Count == 0)
            {
                MessageBox.Show("File không có dữ liệu.", "Nhập file", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var imported = rows.Select((r, i) => ModelMappingRow.From(new ModelMapping
            {
                SortOrder = i + 1,
                GroupName = r.Cells[0].Trim(),
                ModelName = r.Cells[1].Trim(),
                ImagePath = r.Cells[2].Trim()
            })).ToList();

            var missing = imported.Where(r => r.ImageMissing).Select(r => r.ModelName).ToList();
            string message = $"Thay toàn bộ {Rows.Count} dòng đang có bằng {imported.Count} model trong file " +
                             $"({imported.Select(r => r.GroupName).Distinct(StringComparer.OrdinalIgnoreCase).Count()} nhóm)?";
            if (missing.Count > 0)
                message += $"\n\nCảnh báo: {missing.Count} model có đường dẫn ảnh không tìm thấy file (vẫn nhập, ảnh sẽ không hiện):\n" +
                           string.Join(", ", missing.Take(10)) + (missing.Count > 10 ? ", ..." : "");

            if (MessageBox.Show(message, "Xác nhận nhập", MessageBoxButton.YesNo,
                    missing.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            Rows.Clear();
            foreach (var row in imported)
                Rows.Add(row);

            MessageBox.Show($"Đã nhập {imported.Count} model. Kiểm tra lại rồi bấm Lưu để áp dụng.", "Nhập file",
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

    /// <summary>
    /// Chọn ảnh cho dòng đang chọn: chép vào thư mục ModelImages với tên &lt;model&gt;.ext và điền vào cột Đường dẫn ảnh.
    /// </summary>
    [RelayCommand]
    private void ChooseImage()
    {
        if (SelectedRow == null || string.IsNullOrWhiteSpace(SelectedRow.ModelName))
        {
            MessageBox.Show("Chọn 1 dòng (đã có tên model) trong bảng trước.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = ImageFilter, Title = $"Chọn ảnh cho model {SelectedRow.ModelName}" };
        if (dialog.ShowDialog() != true) return;

        try
        {
            Directory.CreateDirectory(ImageFolder);
            var safeName = string.Concat(SelectedRow.ModelName.Trim().Split(Path.GetInvalidFileNameChars()));
            var fileName = safeName + Path.GetExtension(dialog.FileName).ToLowerInvariant();
            var target = Path.Combine(ImageFolder, fileName);

            if (!string.Equals(Path.GetFullPath(dialog.FileName), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
            {
                // Bỏ ảnh cũ của model (có thể khác đuôi file)
                foreach (var ext in ImageExtensions)
                {
                    var old = Path.Combine(ImageFolder, safeName + ext);
                    bool isSource = string.Equals(Path.GetFullPath(old), Path.GetFullPath(dialog.FileName), StringComparison.OrdinalIgnoreCase);
                    if (File.Exists(old) && !isSource) File.Delete(old);
                }
                File.Copy(dialog.FileName, target, true);
            }

            ModelImageProvider.ClearCache();
            SelectedRow.ImagePath = fileName; // tên file trong thư mục ModelImages
            SelectedRow.UpdatePreview();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi chép ảnh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ClearImage()
    {
        if (SelectedRow == null) return;
        SelectedRow.ImagePath = string.Empty;
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

    // ===== HELPERS =====

    /// <summary>
    /// Model bắt buộc + không trùng; nhóm bắt buộc.
    /// </summary>
    private static List<string> Validate(IEnumerable<(int RowNumber, string Group, string Model)> rows)
    {
        var errors = new List<string>();
        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var (rowNumber, group, model) in rows)
        {
            var m = (model ?? string.Empty).Trim();
            var g = (group ?? string.Empty).Trim();

            if (m.Length == 0) { errors.Add($"Dòng {rowNumber}: thiếu tên model"); continue; }
            if (g.Length == 0) errors.Add($"Dòng {rowNumber}: model '{m}' chưa có tên nhóm");
            if (seen.TryGetValue(m, out var first)) errors.Add($"Dòng {rowNumber}: model '{m}' bị trùng với dòng {first}");
            else seen[m] = rowNumber;
        }
        return errors;
    }

    private static void ShowErrors(string title, List<string> errors)
    {
        MessageBox.Show(title + "\n" + string.Join("\n", errors.Take(15)) +
                        (errors.Count > 15 ? $"\n... và {errors.Count - 15} lỗi khác" : ""),
            "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void UpdateSummary()
    {
        int groups = Rows.Select(r => r.GroupName.Trim()).Where(g => g.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        SummaryText = $"{Rows.Count} model / {groups} nhóm. Model không có trong bảng sẽ vào nhóm \"{ModelGroupService.OtherGroupName}\".";
    }
}
