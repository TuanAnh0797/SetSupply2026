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
    public string ModelName { get; init; } = string.Empty;
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
    public string TotalText { get; init; } = string.Empty;
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
