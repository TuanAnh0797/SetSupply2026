using APP.Service;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SetSupply.Helper;
using SetSupply.Models.Database;
using SetSupply.Models.Home;
using SetSupply.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace APP.ViewModels.UserControlViewModels.Home;

/// <summary>
/// Màn hình Home: sản lượng theo nhóm model / model, theo giờ, cho cả ngày hoặc từng ca.
/// </summary>
public partial class UCHomeViewModel : ObservableObject
{
    public const string AllOption = "Tất cả";

    // Độ rộng cột cố định của bảng (đồng bộ với UCHome.xaml)
    public const double GroupColumnWidth = 170;
    public const double PlanColumnWidth = 120;
    public const double ModelColumnWidth = 250;
    public const double TotalColumnWidth = 120;
    public const double MinHourColumnWidth = 46;

    // Màu theo thứ tự nhóm: (màu dòng nhóm, màu nhạt cho dòng model, màu cột biểu đồ)
    private static readonly (string Row, string Tint, string Bar)[] GroupPalette =
    {
        ("#D3E5FB", "#EDF4FD", "#4DA6F5"), // xanh dương
        ("#D2F3DC", "#ECFAF0", "#3CCB6A"), // xanh lá
        ("#FCEFB5", "#FEF8DD", "#F7CB3D"), // vàng
        ("#FBD3E4", "#FDEDF4", "#F47DB6"), // hồng
        ("#E5D9F8", "#F3EDFC", "#B98AF2"), // tím
        ("#D0F0F2", "#EBF9FA", "#36C3CC"), // xanh ngọc
        ("#FDDCC6", "#FEF0E6", "#F7A55A"), // cam
    };

    private static readonly CultureInfo NumberCulture = CultureInfo.InvariantCulture;

    private readonly ProductionDataService _productionService;
    private readonly ModelGroupService _modelGroupService;
    private readonly ProductionPlanService _planService;
    private readonly PLCService _plcService;

    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly HashSet<string> _collapsedGroups = new(StringComparer.OrdinalIgnoreCase);
    private readonly DispatcherTimer _timer;

    // Dữ liệu đã tải cho ngày/ca đang chọn (lọc nhóm/model thực hiện trên bộ nhớ)
    private List<(string Model, int HourIndex)> _records = new();
    private List<(string Model, DateTime Time)> _dayRecords = new();
    private List<(ShiftScopeOption Option, DateTime Start, DateTime End)> _shiftRanges = new();
    // Gán model -> nhóm (Model Group) + thứ tự nhóm ("Khác" luôn cuối)
    private Dictionary<string, ModelMapping> _mappings = new(StringComparer.OrdinalIgnoreCase);
    private List<string> _groupNames = new() { ModelGroupService.OtherGroupName };
    // Kế hoạch (Daily Plan) của ngày đang xem: (tên nhóm, ca) -> số lượng. Rỗng => ngày chưa có kế hoạch
    private Dictionary<(string Group, int Shift), int> _dailyPlans = new();
    private List<DateTime> _hourSlots = new();
    private bool _isFollowingToday = true;
    private bool _suppressReload;

    public ObservableCollection<ShiftScopeOption> ScopeOptions { get; } = new();
    public ObservableCollection<string> GroupFilterOptions { get; } = new() { AllOption };
    public ObservableCollection<string> ModelFilterOptions { get; } = new() { AllOption };
    public ObservableCollection<string> HourHeaders { get; } = new();
    public ObservableCollection<ModelGroupRow> Groups { get; } = new();

    [ObservableProperty]
    private ShiftScopeOption? selectedScope;

    [ObservableProperty]
    private DateTime selectedDate = DateTime.Today;

    [ObservableProperty]
    private bool isDatePickerOpen;

    [ObservableProperty]
    private string selectedGroupFilter = AllOption;

    [ObservableProperty]
    private string selectedModelFilter = AllOption;

    [ObservableProperty]
    private TotalRow totals = new();

    [ObservableProperty]
    private string lastUpdateText = "Cập nhật lần cuối: --";

    [ObservableProperty]
    private bool isPlcConnected;

    // ===== Ô Codeback (mã vừa đọc từ PLC) =====
    // ===== Nhãn cảnh báo dữ liệu test trong ngày đang xem =====
    [ObservableProperty]
    private string testDataText = string.Empty;

    [ObservableProperty]
    private bool hasTestData;

    [ObservableProperty]
    private string lastCodeText = "--";

    [ObservableProperty]
    private string lastCodeTimeText = string.Empty;

    [ObservableProperty]
    private string lastCodeStatusText = "Chưa có dữ liệu";

    [ObservableProperty]
    private Brush lastCodeStatusBrush = Brushes.Gray;

    [ObservableProperty]
    private double tableMinWidth;

    public string SelectedDateText => $"{SelectedDate:dd/MM/yyyy} ({GetDayName(SelectedDate)})";

    public string ConnectionText => IsPlcConnected ? "Đang kết nối" : "Mất kết nối";

    public UCHomeViewModel(ProductionDataService productionService, ModelGroupService modelGroupService,
                           ProductionPlanService planService, PLCService plcService, PlanImportService planImportService)
    {
        _productionService = productionService;
        _modelGroupService = modelGroupService;
        _planService = planService;
        _plcService = plcService;

        ScopeOptions.Add(new ShiftScopeOption { Name = "Cả ngày" });
        selectedScope = ScopeOptions[0];

        IsPlcConnected = _plcService.IsConnected;
        _plcService.DataReceived += PlcService_DataReceived;
        _plcService.ConnectionChanged += connected =>
            Application.Current?.Dispatcher.InvokeAsync(() => IsPlcConnected = connected);
        // Có file kế hoạch mới được import => tải lại kế hoạch
        planImportService.PlanImported += _ => Application.Current?.Dispatcher.InvokeAsync(LoadAsync);
        // Thêm / xóa dữ liệu test hàng loạt => tải lại
        _productionService.DataChanged += () => Application.Current?.Dispatcher.InvokeAsync(LoadAsync);
        _plcService.CodeRead += result =>
            Application.Current?.Dispatcher.InvokeAsync(() => ShowCodeRead(result));

        // Mỗi phút: làm mới giờ hiện tại / tự chuyển sang ngày sản xuất mới
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _timer.Tick += async (_, _) =>
        {
            if (_isFollowingToday) await LoadAsync();
        };
        _timer.Start();
    }

    /// <summary>
    /// Gọi khi màn hình Home được hiển thị: tải lại cài đặt ca, nhóm model, ảnh và dữ liệu.
    /// </summary>
    public async Task ActivateAsync()
    {
        ModelImageProvider.ClearCache();
        await LoadLastCodeAsync();
        await LoadAsync();
    }

    /// <summary>
    /// PLCService đã lưu sản phẩm vào database trước khi phát sự kiện => chỉ cần tải lại.
    /// </summary>
    private void PlcService_DataReceived(string data)
    {
        if (_isFollowingToday)
            Application.Current?.Dispatcher.InvokeAsync(LoadAsync);
    }

    // ===== CODEBACK =====

    private void ShowCodeRead(PlcReadResult result)
    {
        LastCodeText = result.Code.Length > 0 ? result.Code : "(trống)";
        LastCodeTimeText = result.Time.ToString("HH:mm:ss");
        (LastCodeStatusText, var color) = result.Status switch
        {
            PlcReadStatus.Saved => ("Đã lưu", "#1E9E45"),
            PlcReadStatus.Duplicate => ("Trùng", "#E08A00"),
            PlcReadStatus.Invalid => ("Sai mã", "#E53935"),
            _ => ("Lỗi lưu", "#E53935")
        };
        LastCodeStatusBrush = CreateBrush(color);
    }

    /// <summary>
    /// Chưa đọc được mã nào từ khi mở chương trình => hiện bản ghi mới nhất trong database.
    /// </summary>
    private async Task LoadLastCodeAsync()
    {
        if (_plcService.LastRead != null)
        {
            ShowCodeRead(_plcService.LastRead);
            return;
        }

        try
        {
            var latest = await _productionService.GetLatestRecordAsync();
            if (latest == null) return;
            LastCodeText = latest.Codeback;
            LastCodeTimeText = latest.DateTimeInsert.Date == DateTime.Today
                ? latest.DateTimeInsert.ToString("HH:mm:ss")
                : latest.DateTimeInsert.ToString("dd/MM HH:mm:ss");
            LastCodeStatusText = "Gần nhất";
            LastCodeStatusBrush = Brushes.Gray;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Load last code error: {ex.Message}");
        }
    }

    // ===== COMMANDS =====

    [RelayCommand]
    private void GoToToday()
    {
        _isFollowingToday = true;
        IsDatePickerOpen = false;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    // ===== PROPERTY CHANGES =====

    partial void OnSelectedDateChanged(DateTime value)
    {
        OnPropertyChanged(nameof(SelectedDateText));
        IsDatePickerOpen = false;
        if (_suppressReload) return;

        _isFollowingToday = false;
        _ = LoadAsync();
    }

    partial void OnSelectedScopeChanged(ShiftScopeOption? value)
    {
        if (_suppressReload || value == null) return;
        _ = LoadAsync();
    }

    partial void OnSelectedGroupFilterChanged(string value)
    {
        if (_suppressReload) return;
        RebuildModelFilterOptions();
        RebuildTable();
    }

    partial void OnSelectedModelFilterChanged(string value)
    {
        if (_suppressReload) return;
        RebuildTable();
    }

    partial void OnIsPlcConnectedChanged(bool value) => OnPropertyChanged(nameof(ConnectionText));

    // ===== LOAD =====

    private async Task LoadAsync()
    {
        await _loadLock.WaitAsync();
        try
        {
            var mappings = await _modelGroupService.GetMappingsAsync();
            _mappings = ModelGroupService.ToLookup(mappings);
            _groupNames = ModelGroupService.GetGroupNames(mappings);

            _suppressReload = true;
            try
            {
                var currentProductionDate = await _productionService.GetProductionDateAsync(DateTime.Now);
                if (_isFollowingToday)
                    SelectedDate = currentProductionDate;
                else if (SelectedDate == currentProductionDate)
                    _isFollowingToday = true; // chọn lại đúng ngày hiện tại => tiếp tục cập nhật realtime

                // Ngày cũ dùng giờ ca theo lịch sử cài đặt ca của ngày đó
                var shifts = await _productionService.GetShiftsForDateAsync(SelectedDate);
                RebuildScopeOptions(shifts);
            }
            finally
            {
                _suppressReload = false;
            }

            _dailyPlans = (await _planService.GetPlansAsync(SelectedDate))
                .GroupBy(p => (p.GroupName, p.ShiftNumber))
                .ToDictionary(g => (g.Key.GroupName, g.Key.ShiftNumber), g => g.Sum(p => p.Quantity));
            PlanSourceText = HasPlan ? "KH theo ngày" : "Chưa có kế hoạch";

            // Luôn tải cả ngày sản xuất (bảng theo ca ở dưới cần số liệu mọi ca), bảng chính lọc theo ca đang chọn
            var (dayStart, dayEnd) = await _productionService.GetProductionRangeAsync(SelectedDate, null);
            var (start, end) = await _productionService.GetProductionRangeAsync(SelectedDate, SelectedScope?.ShiftNumber);

            _shiftRanges = new List<(ShiftScopeOption Option, DateTime Start, DateTime End)>();
            foreach (var option in ScopeOptions.Where(o => o.ShiftNumber.HasValue))
            {
                var (s, e) = await _productionService.GetProductionRangeAsync(SelectedDate, option.ShiftNumber);
                _shiftRanges.Add((option, s, e));
            }

            var firstSlot = start.Date.AddHours(start.Hour);
            _hourSlots = new List<DateTime>();
            for (var t = firstSlot; t < end; t = t.AddHours(1))
                _hourSlots.Add(t);

            int testCount = await _productionService.CountTestDataAsync(dayStart < start ? dayStart : start, dayEnd > end ? dayEnd : end);
            HasTestData = testCount > 0;
            TestDataText = HasTestData ? $"Có {testCount.ToString("#,0", CultureInfo.InvariantCulture)} bản ghi TEST" : string.Empty;

            _dayRecords = await _productionService.GetModelTimesInRangeAsync(
                dayStart < start ? dayStart : start, dayEnd > end ? dayEnd : end);
            _records = _dayRecords
                .Where(r => r.Time >= start && r.Time < end)
                .Select(r => (r.Model, (int)(r.Time - firstSlot).TotalHours))
                .Where(r => r.Item2 >= 0 && r.Item2 < _hourSlots.Count)
                .ToList();

            HourHeaders.Clear();
            foreach (var slot in _hourSlots)
                HourHeaders.Add($"{slot.Hour}:00");

            TableMinWidth = GroupColumnWidth + PlanColumnWidth + ModelColumnWidth + TotalColumnWidth
                            + _hourSlots.Count * MinHourColumnWidth;

            RebuildGroupFilterOptions();
            RebuildModelFilterOptions();
            RebuildTable();

            LastUpdateText = $"Cập nhật lần cuối: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Home load error: {ex}");
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private void RebuildScopeOptions(List<ShiftSetting> shifts)
    {
        var options = new List<ShiftScopeOption> { new() { Name = "Cả ngày" } };
        options.AddRange(shifts.Select(s => new ShiftScopeOption
        {
            ShiftNumber = s.ShiftNumber,
            Name = $"Ca {s.ShiftNumber}",
            TimeRange = $"{s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm}"
        }));

        bool same = options.Count == ScopeOptions.Count
                    && options.Zip(ScopeOptions).All(p => p.First.ShiftNumber == p.Second.ShiftNumber
                                                          && p.First.TimeRange == p.Second.TimeRange);
        if (same) return;

        int? selectedShift = SelectedScope?.ShiftNumber;
        ScopeOptions.Clear();
        foreach (var option in options)
            ScopeOptions.Add(option);

        SelectedScope = ScopeOptions.FirstOrDefault(o => o.ShiftNumber == selectedShift) ?? ScopeOptions[0];
    }

    // ===== BUILD TABLE (TRÊN BỘ NHỚ) =====

    private string GroupOf(string model) => ModelGroupService.ResolveGroup(model, _mappings);

    private void RebuildGroupFilterOptions()
    {
        var names = _groupNames.ToList();
        if (names.SequenceEqual(GroupFilterOptions.Skip(1))) return;

        var selected = SelectedGroupFilter;
        _suppressReload = true;
        GroupFilterOptions.Clear();
        GroupFilterOptions.Add(AllOption);
        foreach (var name in names)
            GroupFilterOptions.Add(name);
        SelectedGroupFilter = GroupFilterOptions.Contains(selected) ? selected : AllOption;
        _suppressReload = false;
    }

    private void RebuildModelFilterOptions()
    {
        var models = _records.Select(r => r.Model).Distinct()
            .Where(m => SelectedGroupFilter == AllOption || GroupOf(m) == SelectedGroupFilter)
            .OrderBy(m => m)
            .ToList();
        if (models.SequenceEqual(ModelFilterOptions.Skip(1))) return;

        var selected = SelectedModelFilter;
        _suppressReload = true;
        ModelFilterOptions.Clear();
        ModelFilterOptions.Add(AllOption);
        foreach (var model in models)
            ModelFilterOptions.Add(model);
        SelectedModelFilter = ModelFilterOptions.Contains(selected) ? selected : AllOption;
        _suppressReload = false;
    }

    private void RebuildTable()
    {
        var displayGroups = _groupNames;
        int hourCount = _hourSlots.Count;
        var now = DateTime.Now;
        bool[] isFuture = _hourSlots.Select(s => s > now).ToArray();

        // Model -> số lượng theo giờ
        var modelHours = new Dictionary<string, int[]>(StringComparer.Ordinal);
        foreach (var (model, hourIndex) in _records)
        {
            if (!modelHours.TryGetValue(model, out var counts))
                modelHours[model] = counts = new int[hourCount];
            counts[hourIndex]++;
        }

        // Nhóm -> danh sách model
        var groupModels = displayGroups.ToDictionary(g => g, _ => new List<string>(), StringComparer.OrdinalIgnoreCase);
        foreach (var model in modelHours.Keys)
            groupModels[GroupOf(model)].Add(model);

        int? shiftNumber = SelectedScope?.ShiftNumber;
        var activeShiftNumbers = ScopeOptions.Where(o => o.ShiftNumber.HasValue).Select(o => o.ShiftNumber!.Value).ToList();

        var rows = new List<ModelGroupRow>();
        var shownGroups = new List<(string Group, int Total, string BarColor)>();
        var grandHours = new int[hourCount];
        int grandPlan = 0;

        for (int gi = 0; gi < displayGroups.Count; gi++)
        {
            var group = displayGroups[gi];
            if (SelectedGroupFilter != AllOption && group != SelectedGroupFilter) continue;

            var models = groupModels[group]
                .Where(m => SelectedModelFilter == AllOption || m == SelectedModelFilter)
                .OrderBy(m => m)
                .ToList();
            if (SelectedModelFilter != AllOption && models.Count == 0) continue;

            int plan = shiftNumber.HasValue
                ? GetPlan(group, shiftNumber.Value)
                : activeShiftNumbers.Sum(n => GetPlan(group, n));

            // Nhóm "Khác": chỉ hiện khi có dữ liệu hoặc có kế hoạch
            if (ModelGroupService.IsOther(group) && models.Count == 0 && plan == 0) continue;

            var (rowColor, tintColor, barColor) = GroupPalette[gi % GroupPalette.Length];
            var rowBrush = CreateBrush(rowColor);
            var tintBrush = CreateBrush(tintColor);

            var groupHours = new int[hourCount];
            var children = new List<ModelRow>();
            foreach (var model in models)
            {
                var counts = modelHours[model];
                for (int h = 0; h < hourCount; h++) groupHours[h] += counts[h];

                children.Add(new ModelRow
                {
                    ModelName = model,
                    Image = ModelImageProvider.Get(model, group, _mappings.TryGetValue(model, out var map) ? map.ImagePath : null),
                    TintBrush = tintBrush,
                    Hours = FormatHours(counts, isFuture),
                    TotalText = FormatNumber(counts.Sum())
                });
            }

            for (int h = 0; h < hourCount; h++) grandHours[h] += groupHours[h];
            grandPlan += plan;
            shownGroups.Add((group, groupHours.Sum(), barColor));

            rows.Add(new ModelGroupRow(OnGroupToggled)
            {
                Name = group,
                PlanText = FormatPlan(plan),
                RowBrush = rowBrush,
                TintBrush = tintBrush,
                Hours = FormatHours(groupHours, isFuture),
                TotalText = FormatNumber(groupHours.Sum()),
                Children = children,
                IsExpanded = !_collapsedGroups.Contains(group)
            });
        }

        Groups.Clear();
        foreach (var row in rows)
            Groups.Add(row);

        Totals = new TotalRow
        {
            PlanText = FormatPlan(grandPlan),
            Hours = FormatHours(grandHours, isFuture),
            TotalText = FormatNumber(grandHours.Sum())
        };

        UpdateBottomPanels(displayGroups, shownGroups, grandHours, isFuture, grandPlan);
    }

    /// <summary>
    /// Kế hoạch của nhóm trong 1 ca (Daily Plan). Không có kế hoạch => 0; số thực tế vẫn đếm bình thường.
    /// </summary>
    private int GetPlan(string group, int shiftNumber)
    {
        return _dailyPlans.TryGetValue((group, shiftNumber), out var quantity) ? quantity : 0;
    }

    private bool HasPlan => _dailyPlans.Count > 0;

    /// <summary>
    /// Ngày chưa có kế hoạch: hiện "-" thay vì 0.
    /// </summary>
    private string FormatPlan(int plan) => HasPlan ? FormatNumber(plan) : "-";

    private void OnGroupToggled(ModelGroupRow row)
    {
        if (row.IsExpanded) _collapsedGroups.Remove(row.Name);
        else _collapsedGroups.Add(row.Name);
    }

    // ===== HELPERS =====

    private static IReadOnlyList<string> FormatHours(int[] counts, bool[] isFuture)
    {
        // Giờ chưa tới: để trống
        return counts.Select((c, i) => isFuture[i] && c == 0 ? string.Empty : FormatNumber(c)).ToList();
    }

    private static string FormatNumber(int value) => value.ToString("#,0", NumberCulture);

    private static Brush CreateBrush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }

    private static string GetDayName(DateTime date) => date.DayOfWeek switch
    {
        DayOfWeek.Sunday => "Chủ nhật",
        DayOfWeek.Monday => "Thứ 2",
        DayOfWeek.Tuesday => "Thứ 3",
        DayOfWeek.Wednesday => "Thứ 4",
        DayOfWeek.Thursday => "Thứ 5",
        DayOfWeek.Friday => "Thứ 6",
        _ => "Thứ 7"
    };
}
