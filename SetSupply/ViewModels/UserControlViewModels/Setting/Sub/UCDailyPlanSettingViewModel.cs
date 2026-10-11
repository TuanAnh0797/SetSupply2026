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
/// Kế hoạch sản lượng theo ngày / theo ca cho từng nhóm model. Có Import (nhiều ngày) / Export (khoảng ngày) Excel, CSV.
/// Ngày không có kế hoạch: Home vẫn đếm thực tế, cột kế hoạch để trống.
/// </summary>
public partial class UCDailyPlanSettingViewModel : ObservableObject
{
    private static readonly string[] FileHeaders = { "Ngày", "Nhóm model", "Kế hoạch Ca 1", "Kế hoạch Ca 2", "Kế hoạch Ca 3" };

    private readonly ModelGroupService _modelGroupService;
    private readonly ProductionPlanService _planService;
    private readonly ProductionDataService _productionService;
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
                                       ProductionDataService productionService)
    {
        _modelGroupService = modelGroupService;
        _planService = planService;
        _productionService = productionService;
    }

    partial void OnPlanDateChanged(DateTime value) => _ = Load();

    [RelayCommand]
    public async Task Load()
    {
        if (_isLoading) return;
        _isLoading = true;
        try
        {
            var groups = await _modelGroupService.GetAllAsync();
            var plans = await _planService.GetPlansAsync(PlanDate);

            int Value(string group, int shift) =>
                plans.FirstOrDefault(p => p.GroupName == group && p.ShiftNumber == shift)?.Quantity ?? 0;

            SetRows(groups.Select(g => new DailyPlanRow
            {
                GroupName = g.Name,
                Shift1 = Value(g.Name, 1),
                Shift2 = Value(g.Name, 2),
                Shift3 = Value(g.Name, 3)
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
            var groups = (await _modelGroupService.GetAllAsync()).Select(g => g.Name).ToList();
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
    /// Nhập kế hoạch nhiều ngày. Cột: Ngày | Nhóm model | Kế hoạch Ca 1 | Kế hoạch Ca 2 | Kế hoạch Ca 3.
    /// Mỗi ngày có trong file sẽ được GHI ĐÈ toàn bộ kế hoạch; ngày không có trong file giữ nguyên.
    /// </summary>
    [RelayCommand]
    private async Task Import()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = SpreadsheetHelper.FileFilter, Title = "Nhập kế hoạch theo ngày" };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var rows = SpreadsheetHelper.Read(dialog.FileName, FileHeaders.Length);
            var errors = new List<string>();
            var byDate = new Dictionary<DateTime, List<ProductionPlan>>();
            var seen = new HashSet<(DateTime, string)>();

            foreach (var (rowNumber, cells) in rows)
            {
                if (!SpreadsheetHelper.TryParseDate(cells[0], out var date))
                {
                    errors.Add($"Dòng {rowNumber}: ngày '{cells[0]}' không hợp lệ (dùng dd/MM/yyyy)");
                    continue;
                }

                string group = cells[1].Trim();
                if (group.Length == 0)
                {
                    errors.Add($"Dòng {rowNumber}: thiếu tên nhóm model");
                    continue;
                }

                if (!seen.Add((date, group.ToUpperInvariant())))
                {
                    errors.Add($"Dòng {rowNumber}: nhóm '{group}' ngày {date:dd/MM/yyyy} bị lặp");
                    continue;
                }

                var quantities = new int[3];
                bool ok = true;
                for (int i = 0; i < 3; i++)
                {
                    if (!SpreadsheetHelper.TryParseQuantity(cells[2 + i], out quantities[i]))
                    {
                        errors.Add($"Dòng {rowNumber}: '{FileHeaders[2 + i]}' = '{cells[2 + i]}' không phải số nguyên >= 0");
                        ok = false;
                        break;
                    }
                }
                if (!ok) continue;

                if (!byDate.TryGetValue(date, out var list))
                    byDate[date] = list = new List<ProductionPlan>();
                for (int i = 0; i < 3; i++)
                    list.Add(new ProductionPlan { PlanDate = date, GroupName = group, ShiftNumber = i + 1, Quantity = quantities[i] });
            }

            if (errors.Count > 0)
            {
                MessageBox.Show("File có lỗi, chưa nhập:\n" + string.Join("\n", errors.Take(15)) +
                                (errors.Count > 15 ? $"\n... và {errors.Count - 15} lỗi khác" : ""),
                    "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (byDate.Count == 0)
            {
                MessageBox.Show("File không có dữ liệu.", "Nhập file", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Tên nhóm khớp không phân biệt hoa/thường với nhóm đang cài đặt => chuẩn hóa theo tên đã cài đặt
            var groupNames = (await _modelGroupService.GetAllAsync()).Select(g => g.Name).ToList();
            var unknown = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var plan in byDate.Values.SelectMany(p => p))
            {
                var match = groupNames.FirstOrDefault(n => string.Equals(n, plan.GroupName, StringComparison.OrdinalIgnoreCase));
                if (match != null) plan.GroupName = match;
                else unknown.Add(plan.GroupName);
            }

            string message = $"Nhập kế hoạch cho {byDate.Count} ngày ({byDate.Keys.Min():dd/MM/yyyy} - {byDate.Keys.Max():dd/MM/yyyy}).\n" +
                             "Kế hoạch đang có của các ngày này sẽ bị GHI ĐÈ.";
            if (unknown.Count > 0)
                message += $"\n\nCảnh báo: {unknown.Count} nhóm chưa có trong Model Group (sẽ không hiện ở Home cho tới khi tạo nhóm):\n" +
                           string.Join(", ", unknown.Take(10)) + (unknown.Count > 10 ? ", ..." : "");
            message += "\n\nTiếp tục?";

            if (MessageBox.Show(message, "Xác nhận nhập kế hoạch", MessageBoxButton.YesNo,
                    unknown.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            await _planService.SaveDaysAsync(byDate);
            await Load();
            MessageBox.Show($"Đã nhập kế hoạch {byDate.Count} ngày.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
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
