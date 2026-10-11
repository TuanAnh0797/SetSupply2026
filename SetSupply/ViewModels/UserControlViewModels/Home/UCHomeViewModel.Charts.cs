using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
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
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace APP.ViewModels.UserControlViewModels.Home;

/// <summary>
/// Các panel phía dưới bảng Home: tổng quan, biểu đồ theo nhóm model, biểu đồ theo giờ, bảng theo ca.
/// Số liệu lấy theo đúng ngày / ca / bộ lọc nhóm-model đang chọn (khớp với dòng Tổng của bảng).
/// </summary>
public partial class UCHomeViewModel
{
    private static readonly SKColor LineColor = new(0x1F, 0x7B, 0xEA);

    public ObservableCollection<GroupBarItem> GroupBars { get; } = new();
    public ObservableCollection<string> GroupAxisLabels { get; } = new();
    public ObservableCollection<ShiftSummaryRow> ShiftSummaries { get; } = new();

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

    [ObservableProperty]
    private string achievementText = "--";

    [ObservableProperty]
    private Brush achievementBrush = Brushes.Gray;

    [ObservableProperty]
    private string groupChartTitle = "Sản lượng theo nhóm model (Cả ngày)";

    [ObservableProperty]
    private ISeries[] hourlySeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] hourlyXAxes = { new Axis() };

    [ObservableProperty]
    private Axis[] hourlyYAxes = { new Axis { MinLimit = 0 } };

    private void UpdateBottomPanels(
        List<ModelGroup> displayGroups,
        List<(ModelGroup Group, int Total, string BarColor)> shownGroups,
        int[] grandHours,
        bool[] isFuture,
        int grandPlan)
    {
        bool isShift = SelectedScope?.ShiftNumber.HasValue == true;
        string scopeName = isShift ? SelectedScope!.Name : "Cả ngày";

        // ===== Tổng quan =====
        int actual = grandHours.Sum();
        OverviewTitle = $"Tổng quan ngày {SelectedDate:dd/MM/yyyy}" + (isShift ? $" - {scopeName}" : "");
        PlanTotalText = FormatNumber(grandPlan);
        ActualTotalText = FormatNumber(actual);
        double percent = grandPlan > 0 ? actual * 100.0 / grandPlan : 0;
        AchievementPercent = percent;
        AchievementText = grandPlan > 0 ? FormatPercent(percent) : "--";
        AchievementBrush = grandPlan > 0 ? GetRatioColors(percent).Strong : Brushes.LightGray;

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
                Name = group.Name,
                ValueText = FormatNumber(total),
                BarBrush = CreateBrush(barColor),
                BarLength = new GridLength(total, GridUnitType.Star),
                RestLength = new GridLength(axisMax - total, GridUnitType.Star)
            });
        }

        GroupAxisLabels.Clear();
        for (int i = 0; i <= 4; i++)
            GroupAxisLabels.Add(FormatNumber((int)(step * i)));

        // ===== Biểu đồ theo giờ =====
        UpdateHourlyChart(grandHours, isFuture);

        // ===== Bảng theo ca =====
        UpdateShiftSummaries(displayGroups, shownGroups);
    }

    private void UpdateHourlyChart(int[] grandHours, bool[] isFuture)
    {
        var values = grandHours.Select((c, i) => isFuture[i] && c == 0 ? (double?)null : c).ToArray();
        var labels = HourHeaders.ToArray();
        int labelEvery = labels.Length > 12 ? 2 : 1;

        HourlySeries = new ISeries[]
        {
            new LineSeries<double?>
            {
                Name = "Sản lượng",
                Values = values,
                Stroke = new SolidColorPaint(LineColor, 2.5f),
                Fill = new LinearGradientPaint(
                    new[] { LineColor.WithAlpha(90), LineColor.WithAlpha(10) },
                    new SKPoint(0.5f, 0), new SKPoint(0.5f, 1)),
                GeometrySize = 9,
                GeometryStroke = new SolidColorPaint(LineColor, 2.5f),
                GeometryFill = new SolidColorPaint(LineColor),
                LineSmoothness = 0
            }
        };

        HourlyXAxes = new[]
        {
            new Axis
            {
                // Không dùng Labels (sẽ bỏ qua Labeler) => tự lọc nhãn để không chồng chữ khi xem cả ngày
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
                LabelsPaint = new SolidColorPaint(new SKColor(0x5B, 0x6B, 0x82)),
                SeparatorsPaint = new SolidColorPaint(new SKColor(0xE3, 0xE9, 0xF2), 1)
            }
        };

        HourlyYAxes = new[]
        {
            new Axis
            {
                MinLimit = 0,
                TextSize = 12,
                Labeler = v => v.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture),
                LabelsPaint = new SolidColorPaint(new SKColor(0x5B, 0x6B, 0x82)),
                SeparatorsPaint = new SolidColorPaint(new SKColor(0xDC, 0xE4, 0xEF), 1)
                {
                    PathEffect = new DashEffect(new float[] { 3, 3 })
                }
            }
        };
    }

    private void UpdateShiftSummaries(
        List<ModelGroup> displayGroups,
        List<(ModelGroup Group, int Total, string BarColor)> shownGroups)
    {
        var shownSet = shownGroups.Select(g => g.Group).ToHashSet();
        var passCache = new Dictionary<string, bool>(StringComparer.Ordinal);

        bool Passes(string model)
        {
            if (passCache.TryGetValue(model, out var ok)) return ok;
            var group = ModelGroupService.Match(model, displayGroups);
            ok = group != null && shownSet.Contains(group)
                 && (SelectedModelFilter == AllOption || model == SelectedModelFilter);
            passCache[model] = ok;
            return ok;
        }

        ShiftSummaries.Clear();
        int totalPlan = 0, totalActual = 0;

        foreach (var (option, start, end) in _shiftRanges)
        {
            int plan = shownGroups.Sum(g => GetPlan(g.Group, option.ShiftNumber!.Value));
            int actual = _dayRecords.Count(r => r.Time >= start && r.Time < end && Passes(r.Model));
            totalPlan += plan;
            totalActual += actual;
            ShiftSummaries.Add(CreateSummary(option.Name, $"({option.TimeRange})", plan, actual, start > DateTime.Now));
        }

        ShiftSummaryTotal = CreateSummary("Tổng", string.Empty, totalPlan, totalActual);
    }

    private static ShiftSummaryRow CreateSummary(string name, string timeRange, int plan, int actual, bool notStarted = false)
    {
        double percent = plan > 0 ? actual * 100.0 / plan : 0;
        bool hasValue = plan > 0 && !notStarted; // ca chưa bắt đầu: không tô màu đỏ 0%
        var colors = GetRatioColors(percent);
        return new ShiftSummaryRow
        {
            Name = name,
            TimeRange = timeRange,
            PlanText = FormatNumber(plan),
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
        percent.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "%";

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
