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
    public const double SttColumnWidth = 70;
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
    private static readonly Brush ZebraBrush = CreateFrozenBrush("#F5F8FD");

    private static Brush CreateFrozenBrush(string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        brush.Freeze();
        return brush;
    }

    private readonly ProductionDataService _productionService;
    private readonly ModelGroupService _modelGroupService;
    private readonly ProductionPlanService _planService;
    private readonly PLCService _plcService;
    private readonly AppSettingService _appSettings;
    private readonly ShiftScheduleService _scheduleService;
    private bool _viewModeLoaded;

    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly HashSet<string> _collapsedGroups = new(StringComparer.OrdinalIgnoreCase);
    private readonly DispatcherTimer _timer;

    // Dữ liệu đã tải cho ngày/ca đang chọn (lọc nhóm/model thực hiện trên bộ nhớ)
    private List<(string Model, int HourIndex)> _records = new();
    private List<(string Model, DateTime Time)> _dayRecords = new();
    // Các ca đang dùng của ngày đang xem (giờ ca + giờ nghỉ theo đúng cài đặt của ngày đó)
    private List<(ShiftScopeOption Option, DateTime Start, DateTime End, ShiftDefinition Def)> _shiftRanges = new();
    // Khoảng thời gian của phạm vi đang xem (cả ngày / 1 ca)
    private DateTime _scopeStart, _scopeEnd;
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
    /// <summary>Chế độ danh sách model: STT | Model | giờ | Tổng (không chia nhóm).</summary>
    public ObservableCollection<ModelRow> FlatRows { get; } = new();

    /// <summary>
    /// false: bảng theo nhóm model (mặc định). true: danh sách model phẳng có STT.
    /// Chỉ đổi bảng sản lượng; các panel phía dưới giữ nguyên. Lưu vào AppSettings "Home.ViewMode".
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGroupMode))]
    private bool isModelListMode;

    public bool IsGroupMode
    {
        get => !IsModelListMode;
        set => IsModelListMode = !value;
    }

    [ObservableProperty]
    private bool isTableEmpty = true;

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

    /// <summary>
    /// false (mặc định): chỉ hiện nhóm có sản lượng thực tế trong ngày / ca đang xem.
    /// true: hiện thêm nhóm CÓ KẾ HOẠCH (trong ngày / ca đang xem) nhưng chưa có sản lượng.
    /// Ngày chưa import kế hoạch => vẫn chỉ hiện nhóm có sản lượng thực tế.
    /// </summary>
    [ObservableProperty]
    private bool showEmptyGroups;

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
                           ProductionPlanService planService, PLCService plcService, PlanImportService planImportService,
                           AppSettingService appSettings, ShiftScheduleService scheduleService)
    {
        _appSettings = appSettings;
        _scheduleService = scheduleService;
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
        // Lưu cài đặt ca / giờ nghỉ => tính lại kế hoạch theo giờ
        _scheduleService.ScheduleChanged += () => Application.Current?.Dispatcher.InvokeAsync(LoadAsync);
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
        if (!_viewModeLoaded)
        {
            _viewModeLoaded = true;
            _suppressViewModeSave = true;
            IsModelListMode = _appSettings.Get(AppSettingService.HomeViewModeKey, "group") == "model";
            _suppressViewModeSave = false;
            LoadChartInterval();
        }
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

    partial void OnShowEmptyGroupsChanged(bool value)
    {
        RebuildGroupFilterOptions();
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

                // Giờ ca + giờ nghỉ theo đúng cài đặt của ngày đang xem (ngày cũ: bản chụp của ngày đó)
                var shifts = await _scheduleService.GetForDateAsync(SelectedDate);
                RebuildScopeOptions(shifts);

                _shiftRanges = new List<(ShiftScopeOption Option, DateTime Start, DateTime End, ShiftDefinition Def)>();
                foreach (var option in ScopeOptions.Where(o => o.ShiftNumber.HasValue))
                {
                    var def = shifts.First(d => d.ShiftNumber == option.ShiftNumber);
                    var (shiftStart, shiftEnd) = def.GetRange(SelectedDate);
                    _shiftRanges.Add((option, shiftStart, shiftEnd, def));
                }
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
            var (dayStart, dayEnd) = _shiftRanges.Count > 0
                ? (_shiftRanges.Min(r => r.Start), _shiftRanges.Max(r => r.End))
                : (SelectedDate.Date, SelectedDate.Date.AddDays(1));
            var (start, end) = (dayStart, dayEnd);
            foreach (var r in _shiftRanges)
                if (r.Option.ShiftNumber == SelectedScope?.ShiftNumber)
                    (start, end) = (r.Start, r.End);
            _scopeStart = start;
            _scopeEnd = end;

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

            UpdateTableMinWidth();

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

    private bool _suppressViewModeSave;

    partial void OnIsModelListModeChanged(bool value)
    {
        UpdateTableMinWidth();
        UpdateTableEmpty();
        if (_suppressViewModeSave) return;
        _ = SaveViewModeAsync(value);
    }

    private async Task SaveViewModeAsync(bool modelList)
    {
        try
        {
            await _appSettings.SetAsync(AppSettingService.HomeViewModeKey, modelList ? "model" : "group");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Save Home view mode error: {ex.Message}");
        }
    }

    private void UpdateTableMinWidth()
    {
        double fixedWidth = IsModelListMode
            ? SttColumnWidth + ModelColumnWidth + TotalColumnWidth
            : GroupColumnWidth + PlanColumnWidth + ModelColumnWidth + TotalColumnWidth;
        TableMinWidth = fixedWidth + _hourSlots.Count * MinHourColumnWidth;
    }

    private void UpdateTableEmpty() =>
        IsTableEmpty = IsModelListMode ? FlatRows.Count == 0 : Groups.Count == 0;

    private void RebuildScopeOptions(List<ShiftDefinition> shifts)
    {
        var options = new List<ShiftScopeOption> { new() { Name = "Cả ngày" } };
        options.AddRange(shifts.Select(s => new ShiftScopeOption
        {
            ShiftNumber = s.ShiftNumber,
            Name = $"Ca {s.ShiftNumber}",
            TimeRange = s.TimeRangeText,
            BreaksText = s.BreaksText
        }));

        bool same = options.Count == ScopeOptions.Count
                    && options.Zip(ScopeOptions).All(p => p.First.ShiftNumber == p.Second.ShiftNumber
                                                          && p.First.TimeRange == p.Second.TimeRange
                                                          && p.First.BreaksText == p.Second.BreaksText);
        if (same) return;

        int? selectedShift = SelectedScope?.ShiftNumber;
        ScopeOptions.Clear();
        foreach (var option in options)
            ScopeOptions.Add(option);

        SelectedScope = ScopeOptions.FirstOrDefault(o => o.ShiftNumber == selectedShift) ?? ScopeOptions[0];
    }

    // ===== BUILD TABLE (TRÊN BỘ NHỚ) =====

    private string GroupOf(string model) => ModelGroupService.ResolveGroup(model, _mappings);

    /// <summary>
    /// Nhóm có sản lượng thực tế trong ngày / ca đang xem.
    /// </summary>
    /// <summary>
    /// Kế hoạch của nhóm theo phạm vi đang xem (cả ngày = cộng các ca đang dùng).
    /// </summary>
    private int PlanInScope(string group)
    {
        int? shiftNumber = SelectedScope?.ShiftNumber;
        return shiftNumber.HasValue
            ? GetPlan(group, shiftNumber.Value)
            : ScopeOptions.Where(o => o.ShiftNumber.HasValue).Sum(o => GetPlan(group, o.ShiftNumber!.Value));
    }

    /// <summary>
    /// Nhóm được hiện: có sản lượng thực tế, hoặc (khi tích ô) có kế hoạch trong phạm vi đang xem.
    /// </summary>
    private bool IsGroupVisible(string group, HashSet<string> withData) =>
        withData.Contains(group) || (ShowEmptyGroups && PlanInScope(group) > 0);

    /// <summary>
    /// Danh sách nhóm theo thứ tự hiển thị: nhóm trong Model Group + nhóm chỉ có trong kế hoạch (nếu có), "Khác" luôn cuối.
    /// </summary>
    private List<string> GetAllGroupNames()
    {
        var names = _groupNames.Where(g => !ModelGroupService.IsOther(g)).ToList();
        var known = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
        foreach (var planGroup in _dailyPlans.Keys.Select(k => k.Group).Distinct(StringComparer.OrdinalIgnoreCase))
            if (!ModelGroupService.IsOther(planGroup) && known.Add(planGroup))
                names.Add(planGroup);
        names.Add(ModelGroupService.OtherGroupName);
        return names;
    }

    private HashSet<string> GroupsWithData() =>
        _records.Select(r => GroupOf(r.Model)).ToHashSet(StringComparer.OrdinalIgnoreCase);

    private void RebuildGroupFilterOptions()
    {
        var withData = GroupsWithData();
        var names = GetAllGroupNames().Where(g => IsGroupVisible(g, withData)).ToList();
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
        var displayGroups = GetAllGroupNames();
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
        var flatSource = new List<ModelRow>();
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

            // Mặc định chỉ hiện nhóm có sản lượng thực tế. Nhóm chưa có sản lượng: chỉ hiện khi tích ô VÀ nhóm có kế hoạch trong phạm vi đang xem
            if (models.Count == 0 && !(ShowEmptyGroups && plan > 0)) continue;

            // Màu theo thứ tự các nhóm đang hiện
            var (rowColor, tintColor, barColor) = GroupPalette[rows.Count % GroupPalette.Length];
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

            flatSource.AddRange(children);
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
                // 1 model: gộp vào dòng nhóm, không tốn thêm 1 dòng con
                Children = children.Count > 1 ? children : Array.Empty<ModelRow>(),
                SingleModel = children.Count == 1 ? children[0] : null,
                IsExpanded = !_collapsedGroups.Contains(group)
            });
        }

        Groups.Clear();
        foreach (var row in rows)
            Groups.Add(row);

        // Chế độ danh sách model: thứ tự theo giờ sản xuất đầu tiên (giờ sớm hơn lên trước)
        var firstSeen = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < _records.Count; i++)
        {
            var (model, hourIndex) = _records[i];
            int key = hourIndex * 1_000_000 + Math.Min(i, 999_999);
            if (!firstSeen.TryGetValue(model, out var current) || key < current)
                firstSeen[model] = key;
        }
        FlatRows.Clear();
        // Vạch màu ở cột STT = màu cột biểu đồ của nhóm
        var groupBarBrush = shownGroups
            .GroupBy(g => g.Group, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => CreateBrush(g.First().BarColor), StringComparer.OrdinalIgnoreCase);
        int stt = 1;
        foreach (var source in flatSource.OrderBy(m => firstSeen.TryGetValue(m.ModelName, out var k) ? k : int.MaxValue))
        {
            FlatRows.Add(new ModelRow
            {
                Stt = stt,
                ModelName = source.ModelName,
                Image = source.Image,
                TintBrush = groupBarBrush.TryGetValue(GroupOf(source.ModelName), out var bar) ? bar : source.TintBrush,
                RowBackground = stt % 2 == 0 ? ZebraBrush : Brushes.White,
                Hours = source.Hours,
                TotalText = source.TotalText
            });
            stt++;
        }
        UpdateTableEmpty();
        UpdateAllExpanded();

        // Kế hoạch theo giờ = kế hoạch ca chia đều cho các giờ của ca (theo số phút thuộc ca)
        var planHours = DistributePlanByHour(shownGroups);
        var actualTexts = FormatHours(grandHours, isFuture);
        var hourCells = new List<HourValueCell>(hourCount);
        for (int h = 0; h < hourCount; h++)
        {
            // Giờ đã kết thúc và có kế hoạch: xanh = đạt, đỏ = thiếu
            Brush color = Brushes.White;
            if (HasPlan && planHours[h] > 0 && _hourSlots[h].AddHours(1) <= now)
                color = grandHours[h] >= planHours[h] ? FooterGoodBrush : FooterBadBrush;
            hourCells.Add(new HourValueCell { Text = actualTexts[h], Foreground = color });
        }

        Totals = new TotalRow
        {
            PlanText = FormatPlan(grandPlan),
            Hours = actualTexts,
            HourCells = hourCells,
            TotalText = FormatNumber(grandHours.Sum()),
            HasPlan = HasPlan,
            PlanHours = planHours.Select(p => p > 0 ? FormatNumber(p) : string.Empty).ToList(),
            PlanTotalText = FormatPlan(grandPlan)
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

    private static readonly Brush FooterGoodBrush = CreateFrozenBrush("#8EF0A4");
    private static readonly Brush FooterBadBrush = CreateFrozenBrush("#FFAFA3");

    /// <summary>
    /// Kế hoạch từng cột giờ (xem <see cref="DistributePlan"/>).
    /// </summary>
    private int[] DistributePlanByHour(List<(string Group, int Total, string BarColor)> shownGroups) =>
        DistributePlan(_hourSlots.Select(t => (t, t.AddHours(1))).ToList(), shownGroups);

    /// <summary>
    /// Chia kế hoạch mỗi ca (các nhóm đang hiện) cho các khoảng thời gian theo SỐ PHÚT LÀM VIỆC
    /// (thuộc ca, trừ giờ nghỉ) nằm trong từng khoảng; làm tròn kiểu "phần dư lớn nhất" để tổng = kế hoạch ca.
    /// Xem 1 ca => chỉ tính ca đó.
    /// </summary>
    private int[] DistributePlan(IReadOnlyList<(DateTime Start, DateTime End)> slots,
                                 List<(string Group, int Total, string BarColor)> shownGroups)
    {
        int n = slots.Count;
        var result = new int[n];
        if (!HasPlan || n == 0) return result;

        foreach (var (option, _, _, def) in ShiftsInScope())
        {
            int plan = shownGroups.Sum(g => GetPlan(g.Group, option.ShiftNumber!.Value));
            if (plan <= 0) continue;

            var minutes = slots.Select(slot => def.WorkMinutes(SelectedDate, slot.Start, slot.End)).ToArray();
            double slotMinutes = minutes.Sum();
            double totalMinutes = def.TotalWorkMinutes(SelectedDate);
            if (totalMinutes <= 0 || slotMinutes <= 0) continue;

            // Các khoảng có thể chỉ phủ 1 phần ca => chỉ phần kế hoạch thuộc các khoảng này
            int planHere = (int)Math.Round(plan * Math.Min(1, slotMinutes / totalMinutes));
            var exact = minutes.Select(m => planHere * m / slotMinutes).ToArray();
            var parts = exact.Select(x => (int)Math.Floor(x)).ToArray();
            int rest = planHere - parts.Sum();
            foreach (int i in Enumerable.Range(0, n).Where(i => minutes[i] > 0)
                         .OrderByDescending(i => exact[i] - parts[i]).ThenBy(i => i).Take(rest))
                parts[i]++;

            for (int i = 0; i < n; i++) result[i] += parts[i];
        }
        return result;
    }

    /// <summary>
    /// Target cộng dồn tới thời điểm <paramref name="time"/>: kế hoạch mỗi ca x (phút làm việc đã qua / tổng phút làm việc của ca).
    /// </summary>
    private double TargetUntil(DateTime time, List<(string Group, int Total, string BarColor)> shownGroups)
    {
        double target = 0;
        foreach (var (option, start, _, def) in ShiftsInScope())
        {
            int plan = shownGroups.Sum(g => GetPlan(g.Group, option.ShiftNumber!.Value));
            if (plan <= 0 || time <= start) continue;
            double total = def.TotalWorkMinutes(SelectedDate);
            if (total <= 0) continue;
            target += plan * Math.Min(1, def.WorkMinutes(SelectedDate, start, time) / total);
        }
        return target;
    }

    /// <summary>Các ca thuộc phạm vi đang xem (cả ngày = mọi ca đang dùng).</summary>
    private IEnumerable<(ShiftScopeOption Option, DateTime Start, DateTime End, ShiftDefinition Def)> ShiftsInScope()
    {
        int? selectedShift = SelectedScope?.ShiftNumber;
        return _shiftRanges.Where(r => !selectedShift.HasValue || r.Option.ShiftNumber == selectedShift);
    }

    /// <summary>
    /// Ngày chưa có kế hoạch: hiện "-" thay vì 0.
    /// </summary>
    private string FormatPlan(int plan) => HasPlan ? FormatNumber(plan) : "-";

    private void OnGroupToggled(ModelGroupRow row)
    {
        if (row.IsExpanded) _collapsedGroups.Remove(row.Name);
        else _collapsedGroups.Add(row.Name);
        UpdateAllExpanded();
    }

    /// <summary>
    /// true = mọi nhóm nhiều model đang mở (nút trên tiêu đề cột hiện "thu gọn tất cả").
    /// </summary>
    [ObservableProperty]
    private bool allGroupsExpanded = true;

    private void UpdateAllExpanded() =>
        AllGroupsExpanded = Groups.Where(g => g.HasChildren).All(g => g.IsExpanded);

    /// <summary>
    /// Mở rộng / thu gọn tất cả nhóm (chỉ còn dòng nhóm => xem được nhiều nhóm hơn).
    /// </summary>
    [RelayCommand]
    private void ToggleAllGroups()
    {
        bool expand = !AllGroupsExpanded;
        foreach (var row in Groups.Where(g => g.HasChildren))
        {
            row.IsExpanded = expand;
            if (expand) _collapsedGroups.Remove(row.Name);
            else _collapsedGroups.Add(row.Name);
        }
        AllGroupsExpanded = expand;
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
