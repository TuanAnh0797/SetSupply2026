using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SetSupply.Helper;
using SetSupply.Models.Database;
using SetSupply.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SetSupply.ViewModels.UserControlViewModels.Setting.Sub;

/// <summary>
/// 1 dòng kế hoạch theo ngày của 1 nhóm model.
/// </summary>
public partial class DailyPlanRow : ObservableObject
{
    public string GroupName { get; init; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Total))]
    private int shift1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Total))]
    private int shift2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Total))]
    private int shift3;

    public int Total => Shift1 + Shift2 + Shift3;
}

/// <summary>
/// Kế hoạch sản lượng theo ngày / theo ca cho từng nhóm model.
/// Import: file kế hoạch sản xuất (sheet "Daily revise", model NR-), thủ công hoặc tự động khi có file mới trong thư mục.
/// Export: khoảng ngày ra Excel / CSV (Ngày | Nhóm model | Kế hoạch Ca 1..3).
/// Ngày không có kế hoạch: Home vẫn đếm thực tế, cột kế hoạch để trống.
/// </summary>
public partial class UCDailyPlanSettingViewModel : ObservableObject
{
    private static readonly string[] FileHeaders = { "Ngày", "Nhóm model", "Kế hoạch Ca 1", "Kế hoạch Ca 2", "Kế hoạch Ca 3" };

    private readonly ModelGroupService _modelGroupService;
    private readonly ProductionPlanService _planService;
    private readonly ProductionDataService _productionService;
    private readonly PlanImportService _planImportService;
    private readonly AppSettingService _settingService;
    private bool _isLoading;

    public ObservableCollection<DailyPlanRow> Rows { get; } = new();

    [ObservableProperty]
    private DateTime planDate = DateTime.Today;

    [ObservableProperty]
    private DateTime exportFrom = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    [ObservableProperty]
    private DateTime exportTo = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1).AddDays(-1);

    [ObservableProperty]
    private string statusText = string.Empty;

    [ObservableProperty]
    private string shiftInfoText = string.Empty;

    [ObservableProperty]
    private string totalText = string.Empty;

    public UCDailyPlanSettingViewModel(ModelGroupService modelGroupService, ProductionPlanService planService,
                                       ProductionDataService productionService,
                                       PlanImportService planImportService, AppSettingService settingService)
    {
        _modelGroupService = modelGroupService;
        _planService = planService;
        _productionService = productionService;
        _planImportService = planImportService;
        _settingService = settingService;
        LoadAutoImportSettings();
        _planImportService.StatusChanged += () =>
            Application.Current?.Dispatcher.InvokeAsync(() => AutoImportStatus = _planImportService.Status);
    }

    partial void OnPlanDateChanged(DateTime value) => _ = Load();

    [RelayCommand]
    public async Task Load()
    {
        if (_isLoading) return;
        _isLoading = true;
        try
        {
            var groups = await _modelGroupService.GetGroupNamesAsync();
            var plans = await _planService.GetPlansAsync(PlanDate);

            int Value(string group, int shift) =>
                plans.FirstOrDefault(p => p.GroupName == group && p.ShiftNumber == shift)?.Quantity ?? 0;

            SetRows(groups.Select(g => new DailyPlanRow
            {
                GroupName = g,
                Shift1 = Value(g, 1),
                Shift2 = Value(g, 2),
                Shift3 = Value(g, 3)
            }));

            StatusText = plans.Any()
                ? "Ngày này ĐÃ CÓ kế hoạch."
                : "Ngày này CHƯA CÓ kế hoạch — màn hình Home vẫn đếm thực tế, cột kế hoạch để trống. Nhập số rồi bấm Lưu, hoặc dùng Import.";

            var shifts = await _productionService.GetShiftsForDateAsync(PlanDate);
            ShiftInfoText = shifts.Any()
                ? "Ca đang dùng: " + string.Join("   |   ", shifts.Select(s => $"Ca {s.ShiftNumber} ({s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm})"))
                : "Không có ca nào đang dùng.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải kế hoạch: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// Copy kế hoạch của ngày gần nhất trước đó (chưa lưu).
    /// </summary>
    [RelayCommand]
    private async Task CopyPrevious()
    {
        var previous = await _planService.GetPreviousPlanDateAsync(PlanDate);
        if (previous == null)
        {
            MessageBox.Show("Chưa có ngày nào trước đó có kế hoạch.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var plans = await _planService.GetPlansAsync(previous.Value);
        foreach (var row in Rows)
        {
            int Get(int shift) => plans.FirstOrDefault(p => p.GroupName == row.GroupName && p.ShiftNumber == shift)?.Quantity ?? 0;
            row.Shift1 = Get(1);
            row.Shift2 = Get(2);
            row.Shift3 = Get(3);
        }
        StatusText = $"Đã copy kế hoạch ngày {previous:dd/MM/yyyy} — bấm Lưu để áp dụng cho ngày này.";
    }

    [RelayCommand]
    private async Task Save()
    {
        if (Rows.Any(r => r.Shift1 < 0 || r.Shift2 < 0 || r.Shift3 < 0))
        {
            MessageBox.Show("Kế hoạch không được âm.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await _planService.SavePlansAsync(PlanDate, ToPlans(Rows));
            await Load();
            MessageBox.Show($"Đã lưu kế hoạch ngày {PlanDate:dd/MM/yyyy}.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi lưu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Xóa kế hoạch của ngày => ngày đó coi như chưa có kế hoạch.
    /// </summary>
    [RelayCommand]
    private async Task Delete()
    {
        if (MessageBox.Show($"Xóa kế hoạch của ngày {PlanDate:dd/MM/yyyy}?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            await _planService.DeletePlansAsync(PlanDate);
            await Load();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi xóa: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ===== IMPORT / EXPORT =====

    /// <summary>
    /// Xuất kế hoạch từ ExportFrom đến ExportTo: mỗi ngày x mỗi nhóm 1 dòng (chưa có kế hoạch = 0).
    /// File xuất ra dùng luôn làm mẫu để sửa rồi Import lại.
    /// </summary>
    [RelayCommand]
    private async Task Export()
    {
        if (ExportTo < ExportFrom)
        {
            MessageBox.Show("Ngày kết thúc phải sau ngày bắt đầu.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if ((ExportTo - ExportFrom).TotalDays > 366)
        {
            MessageBox.Show("Chỉ xuất tối đa 1 năm mỗi lần.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = SpreadsheetHelper.FileFilter,
            FileName = $"DailyPlan_{ExportFrom:yyyyMMdd}-{ExportTo:yyyyMMdd}.xlsx",
            Title = "Xuất kế hoạch theo ngày"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var groups = await _modelGroupService.GetGroupNamesAsync();
            var plans = await _planService.GetPlansInRangeAsync(ExportFrom, ExportTo);
            var lookup = plans.ToDictionary(p => (p.PlanDate.Date, p.GroupName, p.ShiftNumber), p => p.Quantity);

            var rows = new List<object?[]>();
            for (var date = ExportFrom.Date; date <= ExportTo.Date; date = date.AddDays(1))
            {
                // Nhóm đang cài đặt + nhóm cũ còn kế hoạch trong ngày (nếu có)
                var names = groups.Concat(plans.Where(p => p.PlanDate.Date == date).Select(p => p.GroupName))
                                  .Distinct(StringComparer.Ordinal);
                foreach (var name in names)
                {
                    int Get(int shift) => lookup.TryGetValue((date, name, shift), out var q) ? q : 0;
                    rows.Add(new object?[] { date, name, Get(1), Get(2), Get(3) });
                }
            }

            SpreadsheetHelper.Write(dialog.FileName, "DailyPlan", FileHeaders, rows);
            MessageBox.Show($"Đã xuất {rows.Count} dòng ({ExportFrom:dd/MM/yyyy} - {ExportTo:dd/MM/yyyy}) ra file:\n{dialog.FileName}",
                "Xuất file", MessageBoxButton.OK, MessageBoxImage.Information);
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
    /// Import file kế hoạch (định dạng sheet "Daily revise": cột Model, Production Shifts = Plan, khối S.1 | S.2 | S.3 theo ngày).
    /// Chỉ lấy model bắt đầu bằng "NR-", cộng theo nhóm model. Các ngày có trong file bị GHI ĐÈ kế hoạch.
    /// </summary>
    [RelayCommand]
    private async Task Import()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Excel (*.xlsx;*.xlsm)|*.xlsx;*.xlsm",
            Title = "Chọn file kế hoạch sản xuất"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            PlanImportPreview preview;
            try
            {
                preview = await _planImportService.ReadAsync(dialog.FileName);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }

            if (preview.PlansByDate.Count == 0)
            {
                MessageBox.Show("File không có ngày nào để nhập.", "Nhập file", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string message = $"Sheet \"{preview.SheetName}\": {preview.ModelCount} model {PlanFileParser.ModelPrefix}, " +
                             $"{preview.PlansByDate.Count} ngày ({preview.From:dd/MM/yyyy} - {preview.To:dd/MM/yyyy}), tổng {preview.TotalQuantity.ToString("#,0", CultureInfo.InvariantCulture)}.\n" +
                             $"Cột S.1 / S.2 / S.3 => {ShiftMapText(_settingService.PlanShiftMap)}.\n" +
                             "Kế hoạch đang có của các ngày này sẽ bị GHI ĐÈ.";
            if (preview.UnmappedModels.Count > 0)
                message += $"\n\n{preview.UnmappedModels.Count} model chưa có nhóm (tính vào \"{ModelGroupService.OtherGroupName}\"):\n" +
                           string.Join(", ", preview.UnmappedModels.Take(10)) + (preview.UnmappedModels.Count > 10 ? ", ..." : "");
            if (preview.Warnings.Count > 0)
                message += "\n\nCảnh báo:\n" + string.Join("\n", preview.Warnings.Take(8)) + (preview.Warnings.Count > 8 ? "\n..." : "");
            message += "\n\nTiếp tục?";

            if (MessageBox.Show(message, "Xác nhận nhập kế hoạch", MessageBoxButton.YesNo,
                    preview.UnmappedModels.Count > 0 || preview.Warnings.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            await _planImportService.SaveAsync(preview, PlanImportService.SourceManual);
            await Load();
            await LoadLogs();
            MessageBox.Show($"Đã nhập kế hoạch {preview.PlansByDate.Count} ngày.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (IOException ex)
        {
            MessageBox.Show($"Không đọc được file: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi nhập file: {ex.GetBaseException().Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ===== TỰ ĐỘNG IMPORT =====

    public IReadOnlyList<KeyValuePair<int, string>> ShiftChoices { get; } = new List<KeyValuePair<int, string>>
    {
        new(1, "Ca 1"), new(2, "Ca 2"), new(3, "Ca 3"), new(0, "Bỏ qua")
    };

    public ObservableCollection<PlanImportLog> ImportLogs { get; } = new();

    [ObservableProperty]
    private bool autoImportEnabled;

    [ObservableProperty]
    private string autoImportFolder = string.Empty;

    [ObservableProperty]
    private int shiftForS1 = 1;

    [ObservableProperty]
    private int shiftForS2 = 2;

    [ObservableProperty]
    private int shiftForS3 = 3;

    [ObservableProperty]
    private string autoImportStatus = string.Empty;

    /// <summary>Tên sheet đọc trong file kế hoạch (để trống = tự dò).</summary>
    [ObservableProperty]
    private string planSheetName = PlanFileParser.DefaultSheetName;

    private void LoadAutoImportSettings()
    {
        AutoImportEnabled = _settingService.PlanImportEnabled;
        AutoImportFolder = _settingService.PlanImportFolder;
        var map = _settingService.PlanShiftMap;
        ShiftForS1 = map[0];
        ShiftForS2 = map[1];
        ShiftForS3 = map[2];
        PlanSheetName = _settingService.PlanSheetName;
        AutoImportStatus = _planImportService.Status;
    }

    [RelayCommand]
    private async Task LoadLogs()
    {
        try
        {
            var logs = await _planImportService.GetRecentLogsAsync(30);
            ImportLogs.Clear();
            foreach (var log in logs) ImportLogs.Add(log);
            AutoImportStatus = _planImportService.Status;
        }
        catch (Exception ex)
        {
            AutoImportStatus = $"Lỗi đọc nhật ký: {ex.Message}";
        }
    }

    /// <summary>
    /// Chọn thư mục: chọn 1 file bất kỳ trong thư mục chứa file kế hoạch.
    /// </summary>
    [RelayCommand]
    private void BrowseFolder()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Excel (*.xlsx;*.xlsm)|*.xlsx;*.xlsm|Tất cả file (*.*)|*.*",
            Title = "Chọn 1 file bất kỳ trong thư mục chứa file kế hoạch",
            CheckFileExists = true
        };
        if (Directory.Exists(AutoImportFolder)) dialog.InitialDirectory = AutoImportFolder;
        if (dialog.ShowDialog() == true)
            AutoImportFolder = Path.GetDirectoryName(dialog.FileName) ?? AutoImportFolder;
    }

    [RelayCommand]
    private async Task SaveAutoImport()
    {
        var folder = (AutoImportFolder ?? string.Empty).Trim();
        if (AutoImportEnabled && !Directory.Exists(folder))
        {
            MessageBox.Show($"Thư mục không tồn tại:\n{folder}", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await _settingService.SetAsync(AppSettingService.PlanImportFolderKey, folder);
            await _settingService.SetAsync(AppSettingService.PlanShiftMapKey, $"{ShiftForS1},{ShiftForS2},{ShiftForS3}");
            var sheet = (PlanSheetName ?? string.Empty).Trim();
            await _settingService.SetAsync(AppSettingService.PlanSheetNameKey, sheet.Length == 0 ? "*" : sheet);
            await _settingService.SetAsync(AppSettingService.PlanImportEnabledKey, AutoImportEnabled ? "1" : "0");
            _planImportService.Start(); // áp dụng cài đặt mới (bật / tắt / đổi thư mục)
            AutoImportStatus = _planImportService.Status;
            MessageBox.Show(AutoImportEnabled
                    ? "Đã lưu. Chương trình sẽ tự import file kế hoạch mới trong thư mục (kiểm tra mỗi phút)."
                    : "Đã lưu. Tự động import đang TẮT.",
                "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi lưu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task ScanNow()
    {
        if (!_settingService.PlanImportEnabled)
        {
            MessageBox.Show("Tự động import đang TẮT (bật rồi bấm Lưu cài đặt).", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        Mouse.OverrideCursor = Cursors.Wait;
        try
        {
            await _planImportService.ScanAsync();
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
        await Load();
        await LoadLogs();
    }

    private static string ShiftMapText(int[] map) =>
        string.Join(" / ", map.Select(s => s == 0 ? "bỏ qua" : $"Ca {s}"));

    // ===== HELPERS =====

    private static IEnumerable<ProductionPlan> ToPlans(IEnumerable<DailyPlanRow> rows) =>
        rows.SelectMany(r => new[]
        {
            new ProductionPlan { GroupName = r.GroupName, ShiftNumber = 1, Quantity = r.Shift1 },
            new ProductionPlan { GroupName = r.GroupName, ShiftNumber = 2, Quantity = r.Shift2 },
            new ProductionPlan { GroupName = r.GroupName, ShiftNumber = 3, Quantity = r.Shift3 },
        });

    private void SetRows(IEnumerable<DailyPlanRow> rows)
    {
        foreach (var row in Rows) row.PropertyChanged -= Row_PropertyChanged;
        Rows.Clear();
        foreach (var row in rows)
        {
            row.PropertyChanged += Row_PropertyChanged;
            Rows.Add(row);
        }
        UpdateTotal();
    }

    private void Row_PropertyChanged(object? sender, PropertyChangedEventArgs e) => UpdateTotal();

    private void UpdateTotal()
    {
        string F(int v) => v.ToString("#,0", CultureInfo.InvariantCulture);
        int s1 = Rows.Sum(r => r.Shift1), s2 = Rows.Sum(r => r.Shift2), s3 = Rows.Sum(r => r.Shift3);
        TotalText = $"Tổng:   Ca 1 = {F(s1)}   |   Ca 2 = {F(s2)}   |   Ca 3 = {F(s3)}   |   Cả 3 ca = {F(s1 + s2 + s3)}";
    }
}
