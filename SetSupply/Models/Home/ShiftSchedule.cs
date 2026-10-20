using System;
using System.Collections.Generic;
using System.Linq;

namespace SetSupply.Models.Home;

/// <summary>
/// 1 khoảng nghỉ (giờ trong ngày).
/// </summary>
public sealed class BreakDefinition
{
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// 1 ca làm việc + các giờ nghỉ, dùng để tính thời gian làm việc thực tế (chia kế hoạch theo giờ, target đến hiện tại).
/// </summary>
public sealed class ShiftDefinition
{
    public int ShiftNumber { get; set; }
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public bool IsUse { get; set; } = true;
    public List<BreakDefinition> Breaks { get; set; } = new();

    /// <summary>Ca qua đêm: giờ kết thúc &lt;= giờ bắt đầu.</summary>
    public bool IsOvernight => End <= Start;

    public string TimeRangeText => $"{Start:hh\\:mm} - {End:hh\\:mm}";

    public string BreaksText => Breaks.Count == 0
        ? string.Empty
        : string.Join(", ", Breaks.OrderBy(b => b.Start).Select(b => $"{b.Start:hh\\:mm}-{b.End:hh\\:mm}"));

    /// <summary>Thời điểm bắt đầu / kết thúc thực tế của ca trong 1 ngày sản xuất.</summary>
    public (DateTime Start, DateTime End) GetRange(DateTime productionDate)
    {
        var start = productionDate.Date + Start;
        var end = (IsOvernight ? productionDate.Date.AddDays(1) : productionDate.Date) + End;
        return (start, end);
    }

    /// <summary>
    /// Các khoảng nghỉ thực tế (đã đặt đúng ngày, cắt trong phạm vi ca, gộp chỗ chồng nhau).
    /// </summary>
    public List<(DateTime Start, DateTime End)> GetBreakRanges(DateTime productionDate)
    {
        var (shiftStart, shiftEnd) = GetRange(productionDate);
        var ranges = new List<(DateTime Start, DateTime End)>();
        foreach (var b in Breaks)
        {
            var bs = productionDate.Date + b.Start;
            if (bs < shiftStart) bs = bs.AddDays(1);          // nghỉ sau 0:00 của ca qua đêm
            var be = bs.Date + b.End;
            if (be <= bs) be = be.AddDays(1);                 // nghỉ vắt qua 0:00
            if (bs < shiftStart) bs = shiftStart;
            if (be > shiftEnd) be = shiftEnd;
            if (be > bs) ranges.Add((bs, be));
        }

        var merged = new List<(DateTime Start, DateTime End)>();
        foreach (var r in ranges.OrderBy(r => r.Start))
        {
            if (merged.Count > 0 && r.Start <= merged[^1].End)
                merged[^1] = (merged[^1].Start, r.End > merged[^1].End ? r.End : merged[^1].End);
            else
                merged.Add(r);
        }
        return merged;
    }

    /// <summary>
    /// Số phút làm việc (trong ca, trừ giờ nghỉ) nằm trong khoảng [from, to).
    /// </summary>
    public double WorkMinutes(DateTime productionDate, DateTime from, DateTime to)
    {
        var (shiftStart, shiftEnd) = GetRange(productionDate);
        double minutes = Overlap(shiftStart, shiftEnd, from, to);
        if (minutes <= 0) return 0;
        foreach (var (bs, be) in GetBreakRanges(productionDate))
            minutes -= Overlap(bs, be, from, to);
        return Math.Max(0, minutes);
    }

    /// <summary>Tổng số phút làm việc của ca (trừ giờ nghỉ).</summary>
    public double TotalWorkMinutes(DateTime productionDate)
    {
        var (s, e) = GetRange(productionDate);
        return WorkMinutes(productionDate, s, e);
    }

    private static double Overlap(DateTime aStart, DateTime aEnd, DateTime bStart, DateTime bEnd)
    {
        var from = aStart > bStart ? aStart : bStart;
        var to = aEnd < bEnd ? aEnd : bEnd;
        return to > from ? (to - from).TotalMinutes : 0;
    }

    public ShiftDefinition Clone() => new()
    {
        ShiftNumber = ShiftNumber,
        Start = Start,
        End = End,
        IsUse = IsUse,
        Breaks = Breaks.Select(b => new BreakDefinition { Start = b.Start, End = b.End, Name = b.Name }).ToList()
    };
}
