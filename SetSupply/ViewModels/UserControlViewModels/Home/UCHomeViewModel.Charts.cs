using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using SetSupply.Models.Database;
using SetSupply.Models.Home;
using SetSupply.Service;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace APP.ViewModels.UserControlViewModels.Home;

/// <summary>
/// 1 lựa chọn khoảng thời gian của biểu đồ sản lượng (Minutes = 0: tùy chỉnh).
/// </summary>
public sealed record ChartIntervalOption(int Minutes, string Label);

/// <summary>
/// Các panel phía dưới bảng Home: tổng quan, biểu đồ theo nhóm model, biểu đồ theo khoảng thời gian, bảng theo ca.
/// Số liệu lấy theo đúng ngày / ca / bộ lọc nhóm-model đang chọn (khớp với dòng Tổng của bảng).
/// </summary>
public partial class UCHomeViewModel
{
    private static readonly SKColor LineColor = new(0x1F, 0x7B, 0xEA);
    private static readonly SKColor PlanLineColor = new(0xF5, 0x8A, 0x1F);
    private static readonly SKColor TargetLineColor = new(0x8A, 0x99, 0xB0);
    private static readonly SKColor GoodColor = new(0x22, 0xB1, 0x4C);
    private static readonly SKColor BadColor = new(0xE5, 0x39, 0x35);
    private static readonly SKColor AxisTextColor = new(0x5B, 0x6B, 0x82);

    public ObservableCollection<GroupBarItem> GroupBars { get; } = new();
    public ObservableCollection<string> GroupAxisLabels { get; } = new();
    public ObservableCollection<ShiftSummaryRow> ShiftSummaries { get; } = new();

    // Các nhóm đang hiện của lần dựng bảng gần nhất (vẽ lại biểu đồ khi đổi khoảng thời gian)
    private List<(string Group, int Total, string BarColor)> _lastShownGroups = new();

    [ObservableProperty]
    private ShiftSummaryRow shiftSummaryTotal = new();

    [ObservableProperty]
    private string overviewTitle = "Tổng quan ngày";

    [ObservableProperty]
    private string planTotalText = "0";

    [ObservableProperty]
    private string actualTotalText = "0";

    [ObservableProperty]
    private string planSourceText = string.Empty;

    [ObservableProperty]
    private double achievementPercent;

    /// <summary>Tỷ lệ thực tế / kế hoạch cả ngày (ca), VD "Đạt 61.7% KH".</summary>
    [ObservableProperty]
    private string achievementText = "--";

    [ObservableProperty]
    private Brush achievementBrush = Brushes.Gray;

    // ===== Tiến độ so với target đến thời điểm hiện tại =====

    /// <summary>VD "Target đến 14:35", "Target cả ngày".</summary>
    [ObservableProperty]
    private string targetNowLabel = "Target hiện tại";

    [ObservableProperty]
    private string targetNowText = "-";

    /// <summary>Thực tế / target hiện tại (%), vẽ vòng tròn.</summary>
    [ObservableProperty]
    private double progressPercent;

    [ObservableProperty]
    private string progressText = "--";

    [ObservableProperty]
    private Brush progressBrush = Brushes.LightGray;

    /// <summary>VD "Chậm 40 tủ", "Vượt 25 tủ", "Đạt target".</summary>
    [ObservableProperty]
    private string diffText = string.Empty;

    [ObservableProperty]
    private Brush diffBrush = Brushes.Gray;

    [ObservableProperty]
    private ISeries[] cumulativeSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] cumulativeXAxes = { new Axis() };

    [ObservableProperty]
    private Axis[] cumulativeYAxes = { new Axis { MinLimit = 0 } };

    [ObservableProperty]
    private string groupChartTitle = "Sản lượng theo nhóm model (Cả ngày)";

    // ===== Biểu đồ sản lượng theo khoảng thời gian =====

    public IReadOnlyList<ChartIntervalOption> ChartIntervalOptions { get; } = new[]
    {
        new ChartIntervalOption(5, "5 phút"),
        new ChartIntervalOption(10, "10 phút"),
        new ChartIntervalOption(15, "15 phút"),
        new ChartIntervalOption(30, "30 phút"),
        new ChartIntervalOption(60, "1 giờ"),
        new ChartIntervalOption(0, "Tùy chỉnh")
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustomInterval))]
    private ChartIntervalOption? selectedChartInterval;

    public bool IsCustomInterval => SelectedChartInterval?.Minutes == 0;

    /// <summary>Số phút khi chọn "Tùy chỉnh" (1..720).</summary>
    [ObservableProperty]
    private string customIntervalText = "20";

    [ObservableProperty]
    private string hourlyChartTitle = "Sản lượng theo giờ (Tổng)";

    [ObservableProperty]
    private ISeries[] hourlySeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] hourlyXAxes = { new Axis() };

    [ObservableProperty]
    private Axis[] hourlyYAxes = { new Axis { MinLimit = 0 } };

    private bool _loadingInterval;
    // Chưa đọc cài đặt khoảng thời gian => không lưu (tránh ComboBox tự chọn mục đầu ghi đè cài đặt)
    private bool _intervalLoaded;

    private void LoadChartInterval()
    {
        _loadingInterval = true;
        try
        {
            var saved = _appSettings.Get(AppSettingService.HomeChartIntervalKey, "60");
            int.TryParse(saved, out var minutes);
            var option = ChartIntervalOptions.FirstOrDefault(o => o.Minutes == minutes && o.Minutes > 0);
            if (option == null && minutes > 0)
            {
                CustomIntervalText = minutes.ToString(CultureInfo.InvariantCulture);
                option = ChartIntervalOptions.First(o => o.Minutes == 0);
            }
            SelectedChartInterval = option ?? ChartIntervalOptions.First(o => o.Minutes == 60);
        }
        finally
        {
            _loadingInterval = false;
            _intervalLoaded = true;
        }
    }

    /// <summary>Số phút mỗi khoảng của biểu đồ (tùy chỉnh sai => giữ 60).</summary>
    private int ChartIntervalMinutes
    {
        get
        {
            if (SelectedChartInterval == null) return 60;
            if (SelectedChartInterval.Minutes > 0) return SelectedChartInterval.Minutes;
            return int.TryParse(CustomIntervalText?.Trim(), out var m) && m >= 1 && m <= 720 ? m : 60;
        }
    }

    partial void OnSelectedChartIntervalChanged(ChartIntervalOption? value) => OnChartIntervalChanged();

    partial void OnCustomIntervalTextChanged(string value)
    {
        if (IsCustomInterval) OnChartIntervalChanged();
    }

    private void OnChartIntervalChanged()
    {
        if (_loadingInterval || !_intervalLoaded) return;
        UpdateIntervalChart();
        if (IsCustomInterval && !(int.TryParse(CustomIntervalText?.Trim(), out var m) && m >= 1 && m <= 720)) return;
        _ = SaveChartIntervalAsync(ChartIntervalMinutes);
    }

    private async System.Threading.Tasks.Task SaveChartIntervalAsync(int minutes)
    {
        try
        {
            await _appSettings.SetAsync(AppSettingService.HomeChartIntervalKey, minutes.ToString(CultureInfo.InvariantCulture));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Save chart interval error: {ex.Message}");
        }
    }

    private void UpdateBottomPanels(
        List<string> displayGroups,
        List<(string Group, int Total, string BarColor)> shownGroups,
        int[] grandHours,
        bool[] isFuture,
        int grandPlan)
    {
        _lastShownGroups = shownGroups;
        bool isShift = SelectedScope?.ShiftNumber.HasValue == true;
        string scopeName = isShift ? SelectedScope!.Name : "Cả ngày";

        // ===== Tổng quan =====
        int actual = grandHours.Sum();
        OverviewTitle = $"Tổng quan ngày {SelectedDate:dd/MM/yyyy}" + (isShift ? $" - {scopeName}" : "");
        PlanTotalText = FormatPlan(grandPlan);
        ActualTotalText = FormatNumber(actual);
        double percent = grandPlan > 0 ? actual * 100.0 / grandPlan : 0;
        AchievementPercent = percent;
        AchievementText = grandPlan > 0 ? $"Đạt {FormatPercent(percent)} KH" : "--";
        AchievementBrush = grandPlan > 0 ? GetRatioColors(percent).Strong : Brushes.LightGray;
        UpdateTargetProgress(shownGroups, actual, grandPlan);

        // ===== Biểu đồ theo nhóm model =====
        GroupChartTitle = $"Sản lượng theo nhóm model ({scopeName})";
        int max = shownGroups.Select(g => g.Total).DefaultIfEmpty(0).Max();
        double step = NiceStep(max / 4.0);
        double axisMax = step * 4;

        GroupBars.Clear();
        foreach (var (group, total, barColor) in shownGroups)
        {
            GroupBars.Add(new GroupBarItem
            {
                Name = group,
                ValueText = FormatNumber(total),
                BarBrush = CreateBrush(barColor),
                BarLength = new GridLength(total, GridUnitType.Star),
                RestLength = new GridLength(axisMax - total, GridUnitType.Star)
            });
        }

        GroupAxisLabels.Clear();
        for (int i = 0; i <= 4; i++)
            GroupAxisLabels.Add(FormatNumber((int)(step * i)));

        // ===== Biểu đồ theo khoảng thời gian =====
        UpdateIntervalChart();

        // ===== Bảng theo ca =====
        UpdateShiftSummaries(displayGroups, shownGroups);
    }

    /// <summary>
    /// Model có được tính vào tổng không (nhóm đang hiện + bộ lọc model) — khớp dòng Tổng của bảng.
    /// </summary>
    private Func<string, bool> CreateModelFilter(List<(string Group, int Total, string BarColor)> shownGroups)
    {
        var shownSet = shownGroups.Select(g => g.Group).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var cache = new Dictionary<string, bool>(StringComparer.Ordinal);
        return model =>
        {
            if (cache.TryGetValue(model, out var ok)) return ok;
            ok = shownSet.Contains(GroupOf(model)) && (SelectedModelFilter == AllOption || model == SelectedModelFilter);
            cache[model] = ok;
            return ok;
        };
    }

    // ===== TIẾN ĐỘ SO VỚI TARGET HIỆN TẠI =====

    /// <summary>
    /// Target đến thời điểm hiện tại = kế hoạch mỗi ca x phút làm việc đã qua / tổng phút làm việc (trừ giờ nghỉ).
    /// Ngày / ca đã kết thúc => target = toàn bộ kế hoạch.
    /// </summary>
    private void UpdateTargetProgress(List<(string Group, int Total, string BarColor)> shownGroups, int actual, int grandPlan)
    {
        var now = DateTime.Now;
        var at = now < _scopeEnd ? now : _scopeEnd;

        if (!HasPlan || grandPlan <= 0)
        {
            TargetNowLabel = "Target hiện tại";
            TargetNowText = "-";
            ProgressPercent = 0;
            ProgressText = "--";
            ProgressBrush = Brushes.LightGray;
            DiffText = "Chưa có kế hoạch";
            DiffBrush = Brushes.Gray;
            UpdateCumulativeChart(shownGroups, false);
            return;
        }

        TargetNowLabel = now >= _scopeEnd ? "Target (đã kết thúc)"
                       : now <= _scopeStart ? "Target (chưa bắt đầu)"
                       : $"Target đến {now:HH:mm}";
        double target = TargetUntil(at, shownGroups);
        int targetRounded = (int)Math.Round(target);
        TargetNowText = FormatNumber(targetRounded);

        if (targetRounded <= 0)
        {
            ProgressPercent = 0;
            ProgressText = "--";
            ProgressBrush = Brushes.LightGray;
            DiffText = actual > 0 ? $"Đã có {FormatNumber(actual)} tủ" : "Chưa tới giờ làm việc";
            DiffBrush = Brushes.Gray;
        }
        else
        {
            double progress = actual * 100.0 / targetRounded;
            int diff = actual - targetRounded;
            ProgressPercent = progress;
            ProgressText = FormatPercent(progress);
            ProgressBrush = GetRatioColors(progress).Strong;
            DiffText = diff > 0 ? $"Vượt {FormatNumber(diff)} tủ" : diff < 0 ? $"Chậm {FormatNumber(-diff)} tủ" : "Đúng target";
            DiffBrush = diff >= 0 ? CreateBrush("#1E9E45") : CreateBrush("#E53935");
        }

        UpdateCumulativeChart(shownGroups, true, actual >= targetRounded);
    }

    /// <summary>
    /// Biểu đồ cộng dồn: target (đường đứt) và thực tế (tới hiện tại) theo thời gian của ngày / ca đang xem.
    /// </summary>
    private void UpdateCumulativeChart(List<(string Group, int Total, string BarColor)> shownGroups, bool hasPlan, bool onTrack = true)
    {
        if (_scopeEnd <= _scopeStart)
        {
            CumulativeSeries = Array.Empty<ISeries>();
            return;
        }

        var start = _scopeStart;
        double totalMinutes = (_scopeEnd - start).TotalMinutes;
        int step = totalMinutes > 12 * 60 ? 30 : 15;
        var now = DateTime.Now;
        var passes = CreateModelFilter(shownGroups);

        var times = _dayRecords.Where(r => r.Time >= start && r.Time < _scopeEnd && passes(r.Model))
                               .Select(r => r.Time).OrderBy(t => t).ToList();

        var targetPoints = new List<ObservablePoint>();
        var actualPoints = new List<ObservablePoint>();
        int idx = 0;
        for (double m = 0; m <= totalMinutes + 0.001; m += step)
        {
            var t = start.AddMinutes(Math.Min(m, totalMinutes));
            if (hasPlan) targetPoints.Add(new ObservablePoint(m, Math.Round(TargetUntil(t, shownGroups))));
            if (t <= now)
            {
                while (idx < times.Count && times[idx] < t) idx++;
                actualPoints.Add(new ObservablePoint(m, idx));
            }
        }
        // Điểm cuối của đường thực tế = đúng thời điểm hiện tại
        if (now > start && now < _scopeEnd)
        {
            double nowMinutes = (now - start).TotalMinutes;
            actualPoints.Add(new ObservablePoint(nowMinutes, times.Count(x => x <= now)));
        }

        var actualColor = !hasPlan ? LineColor : onTrack ? GoodColor : BadColor;
        var series = new List<ISeries>();
        if (hasPlan)
        {
            series.Add(new LineSeries<ObservablePoint>
            {
                Name = "Target",
                Values = targetPoints,
                Stroke = new SolidColorPaint(TargetLineColor, 2) { PathEffect = new DashEffect(new float[] { 6, 4 }) },
                Fill = null,
                GeometrySize = 0,
                LineSmoothness = 0
            });
        }
        series.Add(new LineSeries<ObservablePoint>
        {
            Name = "Thực tế",
            Values = actualPoints,
            Stroke = new SolidColorPaint(actualColor, 2.5f),
            Fill = new SolidColorPaint(actualColor.WithAlpha(35)),
            GeometrySize = 0,
            LineSmoothness = 0
        });
        CumulativeSeries = series.ToArray();

        double labelStep = totalMinutes > 12 * 60 ? 240 : totalMinutes > 6 * 60 ? 120 : 60;
        CumulativeXAxes = new[]
        {
            new Axis
            {
                MinLimit = 0,
                MaxLimit = totalMinutes,
                MinStep = labelStep,
                ForceStepToMin = true,
                Labeler = v => start.AddMinutes(v).ToString("H:mm", CultureInfo.InvariantCulture),
                TextSize = 11,
                LabelsPaint = new SolidColorPaint(AxisTextColor),
                SeparatorsPaint = new SolidColorPaint(new SKColor(0xE3, 0xE9, 0xF2), 1)
            }
        };
        CumulativeYAxes = new[]
        {
            new Axis
            {
                MinLimit = 0,
                TextSize = 11,
                Labeler = v => v.ToString("#,0", CultureInfo.InvariantCulture),
                LabelsPaint = new SolidColorPaint(AxisTextColor),
                SeparatorsPaint = new SolidColorPaint(new SKColor(0xDC, 0xE4, 0xEF), 1) { PathEffect = new DashEffect(new float[] { 3, 3 }) }
            }
        };
    }

    // ===== BIỂU ĐỒ THEO KHOẢNG THỜI GIAN =====

    /// <summary>
    /// Sản lượng mỗi khoảng N phút (5/10/15/30/60 hoặc tùy chỉnh) trong ngày / ca đang xem, kèm đường kế hoạch
    /// (kế hoạch chia theo phút làm việc, trừ giờ nghỉ).
    /// </summary>
    private void UpdateIntervalChart()
    {
        int interval = ChartIntervalMinutes;
        string label = interval % 60 == 0 ? $"{interval / 60} giờ" : $"{interval} phút";
        HourlyChartTitle = $"Sản lượng theo {label} (Tổng)";

        if (_scopeEnd <= _scopeStart)
        {
            HourlySeries = Array.Empty<ISeries>();
            return;
        }

        // Khoảng chia hết 24h => mốc tròn theo đồng hồ (VD 06:00, 06:15...); còn lại tính từ giờ bắt đầu
        var first = _scopeStart;
        if (1440 % interval == 0)
        {
            var dayBase = _scopeStart.Date;
            first = dayBase.AddMinutes(Math.Floor((_scopeStart - dayBase).TotalMinutes / interval) * interval);
        }
        var buckets = new List<(DateTime Start, DateTime End)>();
        for (var t = first; t < _scopeEnd && buckets.Count < 2000; t = t.AddMinutes(interval))
            buckets.Add((t, t.AddMinutes(interval)));

        var passes = CreateModelFilter(_lastShownGroups);
        var counts = new int[buckets.Count];
        foreach (var (model, time) in _dayRecords)
        {
            if (time < _scopeStart || time >= _scopeEnd || time < first) continue;
            int i = (int)((time - first).TotalMinutes / interval);
            if (i >= 0 && i < counts.Length && passes(model)) counts[i]++;
        }

        var now = DateTime.Now;
        var values = counts.Select((c, i) => buckets[i].Start > now && c == 0 ? (double?)null : c).ToArray();
        var labels = buckets.Select(b => b.Start.ToString("H:mm", CultureInfo.InvariantCulture)).ToArray();
        int labelEvery = Math.Max(1, (int)Math.Ceiling(labels.Length / 12.0));
        bool dense = buckets.Count > 40;

        var series = new List<ISeries>
        {
            new LineSeries<double?>
            {
                Name = "Thực tế",
                Values = values,
                Stroke = new SolidColorPaint(LineColor, dense ? 2f : 2.5f),
                Fill = new LinearGradientPaint(
                    new[] { LineColor.WithAlpha(90), LineColor.WithAlpha(10) },
                    new SKPoint(0.5f, 0), new SKPoint(0.5f, 1)),
                GeometrySize = dense ? 0 : 8,
                GeometryStroke = new SolidColorPaint(LineColor, 2.5f),
                GeometryFill = new SolidColorPaint(LineColor),
                LineSmoothness = 0
            }
        };

        if (HasPlan)
        {
            var plan = DistributePlan(buckets, _lastShownGroups);
            if (plan.Any(p => p > 0))
            {
                series.Add(new StepLineSeries<double?>
                {
                    Name = "Kế hoạch",
                    Values = plan.Select(p => (double?)p).ToArray(),
                    Stroke = new SolidColorPaint(PlanLineColor, 2) { PathEffect = new DashEffect(new float[] { 6, 4 }) },
                    Fill = null,
                    GeometrySize = 0
                });
            }
        }
        HourlySeries = series.ToArray();

        HourlyXAxes = new[]
        {
            new Axis
            {
                // Không dùng Labels (sẽ bỏ qua Labeler) => tự lọc nhãn để không chồng chữ
                Labeler = v =>
                {
                    int i = (int)Math.Round(v);
                    return i >= 0 && i < labels.Length && i % labelEvery == 0 ? labels[i] : string.Empty;
                },
                MinStep = 1,
                ForceStepToMin = true,
                MinLimit = -0.3,
                MaxLimit = labels.Length - 0.7,
                TextSize = 12,
                LabelsPaint = new SolidColorPaint(AxisTextColor),
                SeparatorsPaint = new SolidColorPaint(new SKColor(0xE3, 0xE9, 0xF2), 1)
            }
        };

        HourlyYAxes = new[]
        {
            new Axis
            {
                MinLimit = 0,
                TextSize = 12,
                Labeler = v => v.ToString("#,0", CultureInfo.InvariantCulture),
                LabelsPaint = new SolidColorPaint(AxisTextColor),
                SeparatorsPaint = new SolidColorPaint(new SKColor(0xDC, 0xE4, 0xEF), 1)
                {
                    PathEffect = new DashEffect(new float[] { 3, 3 })
                }
            }
        };
    }

    private void UpdateShiftSummaries(
        List<string> displayGroups,
        List<(string Group, int Total, string BarColor)> shownGroups)
    {
        var passes = CreateModelFilter(shownGroups);

        ShiftSummaries.Clear();
        int totalPlan = 0, totalActual = 0;

        foreach (var (option, start, end, _) in _shiftRanges)
        {
            int plan = shownGroups.Sum(g => GetPlan(g.Group, option.ShiftNumber!.Value));
            int actual = _dayRecords.Count(r => r.Time >= start && r.Time < end && passes(r.Model));
            totalPlan += plan;
            totalActual += actual;
            ShiftSummaries.Add(CreateSummary(option.Name, $"({option.TimeRange})", plan, actual, start > DateTime.Now));
        }

        ShiftSummaryTotal = CreateSummary("Tổng", string.Empty, totalPlan, totalActual);
    }

    private ShiftSummaryRow CreateSummary(string name, string timeRange, int plan, int actual, bool notStarted = false)
    {
        double percent = plan > 0 ? actual * 100.0 / plan : 0;
        bool hasValue = plan > 0 && !notStarted; // ca chưa bắt đầu: không tô màu đỏ 0%
        var colors = GetRatioColors(percent);
        return new ShiftSummaryRow
        {
            Name = name,
            TimeRange = timeRange,
            PlanText = FormatPlan(plan),
            ActualText = FormatNumber(actual),
            RatioText = hasValue ? FormatPercent(percent) : "--",
            RatioBackground = hasValue ? colors.Background : Brushes.Transparent,
            RatioForeground = hasValue ? colors.Foreground : Brushes.Gray
        };
    }

    /// <summary>
    /// Màu theo tỷ lệ đạt: >= 98% xanh lá, >= 90% vàng chanh, còn lại đỏ.
    /// </summary>
    private static (Brush Background, Brush Foreground, Brush Strong) GetRatioColors(double percent)
    {
        if (percent >= 98) return (CreateBrush("#CDEFD0"), CreateBrush("#1E7B34"), CreateBrush("#22B14C"));
        if (percent >= 90) return (CreateBrush("#EEF6CC"), CreateBrush("#5F7F12"), CreateBrush("#F5A623"));
        return (CreateBrush("#FBDAD7"), CreateBrush("#C62828"), CreateBrush("#E53935"));
    }

    private static string FormatPercent(double percent) =>
        percent.ToString("0.0", CultureInfo.InvariantCulture) + "%";

    /// <summary>
    /// Bước trục "đẹp": 1, 2, 2.5, 3, 5 x 10^n. VD 270 => 300 (trục 0..1200).
    /// </summary>
    private static double NiceStep(double raw)
    {
        if (raw <= 0) return 100;
        double magnitude = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        double normalized = raw / magnitude;
        double nice = normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 2.5 ? 2.5 : normalized <= 3 ? 3 : normalized <= 5 ? 5 : 10;
        return Math.Max(1, nice * magnitude);
    }
}
