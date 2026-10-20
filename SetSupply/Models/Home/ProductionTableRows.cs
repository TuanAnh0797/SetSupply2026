using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace SetSupply.Models.Home;

/// <summary>
/// Lựa chọn phạm vi hiển thị trên header: "Cả ngày" hoặc từng ca.
/// </summary>
public class ShiftScopeOption
{
    public int? ShiftNumber { get; init; }
    public string Name { get; init; } = string.Empty;
    public string TimeRange { get; init; } = string.Empty;
    public bool HasTimeRange => !string.IsNullOrEmpty(TimeRange);
    /// <summary>Giờ nghỉ của ca, VD "10:00-10:10, 12:00-12:45".</summary>
    public string BreaksText { get; init; } = string.Empty;
    public string ToolTipText => string.IsNullOrEmpty(BreaksText) ? TimeRange : $"{TimeRange}\nNghỉ: {BreaksText}";
}

/// <summary>
/// Dòng nhóm model (có thể mở rộng/thu gọn để xem từng model).
/// </summary>
public partial class ModelGroupRow : ObservableObject
{
    private readonly Action<ModelGroupRow>? _onToggled;

    public ModelGroupRow(Action<ModelGroupRow>? onToggled = null)
    {
        _onToggled = onToggled;
    }

    public string Name { get; init; } = string.Empty;
    public string PlanText { get; init; } = string.Empty;
    public Brush RowBrush { get; init; } = Brushes.White;
    public Brush TintBrush { get; init; } = Brushes.White;
    public IReadOnlyList<string> Hours { get; init; } = Array.Empty<string>();
    public string TotalText { get; init; } = string.Empty;
    public IReadOnlyList<ModelRow> Children { get; init; } = Array.Empty<ModelRow>();
    public bool HasChildren => Children.Count > 0;

    /// <summary>
    /// Nhóm chỉ có 1 model: hiện model ngay trên dòng nhóm (không có dòng con) để tiết kiệm diện tích.
    /// </summary>
    public ModelRow? SingleModel { get; init; }
    public bool HasSingleModel => SingleModel != null;

    /// <summary>
    /// Nhóm nhiều model: ô Model của dòng nhóm hiện "N model".
    /// </summary>
    public string ModelCountText => Children.Count > 1 ? $"{Children.Count} model" : string.Empty;

    [ObservableProperty]
    private bool isExpanded = true;

    [RelayCommand]
    private void Toggle()
    {
        IsExpanded = !IsExpanded;
        _onToggled?.Invoke(this);
    }
}

/// <summary>
/// Dòng chi tiết 1 model trong nhóm.
/// </summary>
public class ModelRow
{
    /// <summary>Số thứ tự (chỉ dùng ở chế độ danh sách model).</summary>
    public int Stt { get; init; }
    public string ModelName { get; init; } = string.Empty;
    /// <summary>Nền dòng ở chế độ danh sách model (kẻ sọc).</summary>
    public Brush RowBackground { get; init; } = Brushes.White;
    public ImageSource? Image { get; init; }
    public bool HasImage => Image != null;
    public Brush TintBrush { get; init; } = Brushes.White;
    public IReadOnlyList<string> Hours { get; init; } = Array.Empty<string>();
    public string TotalText { get; init; } = string.Empty;
}

/// <summary>
/// Dòng "Tổng" ở cuối bảng.
/// </summary>
public class TotalRow
{
    public string PlanText { get; init; } = string.Empty;
    public IReadOnlyList<string> Hours { get; init; } = Array.Empty<string>();
    /// <summary>Ô giờ của dòng Tổng: màu chữ so với kế hoạch giờ đó (giờ đã xong).</summary>
    public IReadOnlyList<HourValueCell> HourCells { get; init; } = Array.Empty<HourValueCell>();
    public string TotalText { get; init; } = string.Empty;

    /// <summary>Dòng "Kế hoạch" (chỉ hiện khi ngày đang xem có kế hoạch): kế hoạch từng giờ + tổng kế hoạch ca / ngày.</summary>
    public bool HasPlan { get; init; }
    public IReadOnlyList<string> PlanHours { get; init; } = Array.Empty<string>();
    public string PlanTotalText { get; init; } = string.Empty;
}

/// <summary>
/// 1 ô số theo giờ có màu chữ riêng.
/// </summary>
public class HourValueCell
{
    public string Text { get; init; } = string.Empty;
    public Brush Foreground { get; init; } = Brushes.White;
}

/// <summary>
/// 1 cột trong biểu đồ "Sản lượng theo nhóm model" (vẽ bằng Grid: phần cột + phần còn lại theo tỷ lệ *).
/// </summary>
public class GroupBarItem
{
    public string Name { get; init; } = string.Empty;
    public string ValueText { get; init; } = string.Empty;
    public Brush BarBrush { get; init; } = Brushes.SteelBlue;
    public System.Windows.GridLength BarLength { get; init; }
    public System.Windows.GridLength RestLength { get; init; }
}

/// <summary>
/// 1 dòng trong bảng "Sản lượng theo ca".
/// </summary>
public class ShiftSummaryRow
{
    public string Name { get; init; } = string.Empty;
    public string TimeRange { get; init; } = string.Empty;
    public bool HasTimeRange => !string.IsNullOrEmpty(TimeRange);
    public string PlanText { get; init; } = string.Empty;
    public string ActualText { get; init; } = string.Empty;
    public string RatioText { get; init; } = string.Empty;
    public Brush RatioBackground { get; init; } = Brushes.Transparent;
    public Brush RatioForeground { get; init; } = Brushes.Black;
}
