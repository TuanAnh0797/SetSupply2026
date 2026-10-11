using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SetSupply.Models.Database;
using SetSupply.Service;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
/// Cài đặt kế hoạch sản lượng cho từng ngày (ghi đè kế hoạch mặc định của nhóm model).
/// </summary>
public partial class UCDailyPlanSettingViewModel : ObservableObject
{
    private readonly ModelGroupService _modelGroupService;
    private readonly ProductionPlanService _planService;
    private readonly ProductionDataService _productionService;
    private bool _isLoading;

    public ObservableCollection<DailyPlanRow> Rows { get; } = new();

    [ObservableProperty]
    private DateTime planDate = DateTime.Today;

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

            int Value(ModelGroup g, int shift)
            {
                var plan = plans.FirstOrDefault(p => p.GroupName == g.Name && p.ShiftNumber == shift);
                return plan?.Quantity ?? g.GetPlan(shift);
            }

            SetRows(groups.Select(g => new DailyPlanRow
            {
                GroupName = g.Name,
                Shift1 = Value(g, 1),
                Shift2 = Value(g, 2),
                Shift3 = Value(g, 3)
            }));

            StatusText = plans.Any()
                ? "Ngày này đang dùng KẾ HOẠCH RIÊNG."
                : "Ngày này chưa có kế hoạch riêng — đang dùng KẾ HOẠCH MẶC ĐỊNH của nhóm model. Sửa số rồi bấm Lưu để tạo kế hoạch riêng.";

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
    /// Điền lại bằng kế hoạch mặc định của nhóm model (chưa lưu).
    /// </summary>
    [RelayCommand]
    private async Task LoadDefault()
    {
        var groups = await _modelGroupService.GetAllAsync();
        SetRows(groups.Select(g => new DailyPlanRow
        {
            GroupName = g.Name,
            Shift1 = g.PlanShift1,
            Shift2 = g.PlanShift2,
            Shift3 = g.PlanShift3
        }));
        StatusText = "Đã điền kế hoạch mặc định — bấm Lưu để áp dụng cho ngày này.";
    }

    /// <summary>
    /// Copy kế hoạch riêng của ngày gần nhất trước đó (chưa lưu).
    /// </summary>
    [RelayCommand]
    private async Task CopyPrevious()
    {
        var previous = await _planService.GetPreviousPlanDateAsync(PlanDate);
        if (previous == null)
        {
            MessageBox.Show("Chưa có ngày nào trước đó có kế hoạch riêng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var plans = await _planService.GetPlansAsync(previous.Value);
        foreach (var row in Rows)
        {
            int Get(int shift) => plans.FirstOrDefault(p => p.GroupName == row.GroupName && p.ShiftNumber == shift)?.Quantity
                                  ?? (shift == 1 ? row.Shift1 : shift == 2 ? row.Shift2 : row.Shift3);
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
            var plans = Rows.SelectMany(r => new[]
            {
                new ProductionPlan { GroupName = r.GroupName, ShiftNumber = 1, Quantity = r.Shift1 },
                new ProductionPlan { GroupName = r.GroupName, ShiftNumber = 2, Quantity = r.Shift2 },
                new ProductionPlan { GroupName = r.GroupName, ShiftNumber = 3, Quantity = r.Shift3 },
            });
            await _planService.SavePlansAsync(PlanDate, plans);
            await Load();
            MessageBox.Show($"Đã lưu kế hoạch ngày {PlanDate:dd/MM/yyyy}.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi lưu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Xóa kế hoạch riêng => ngày này quay về kế hoạch mặc định.
    /// </summary>
    [RelayCommand]
    private async Task Delete()
    {
        if (MessageBox.Show($"Xóa kế hoạch riêng của ngày {PlanDate:dd/MM/yyyy} và dùng lại kế hoạch mặc định?",
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

    private void SetRows(System.Collections.Generic.IEnumerable<DailyPlanRow> rows)
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
