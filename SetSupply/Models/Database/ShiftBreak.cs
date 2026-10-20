using System;

namespace SetSupply.Models.Database;

/// <summary>
/// 1 khoảng nghỉ trong ca (cài đặt hiện tại). Kế hoạch theo giờ chỉ chia cho thời gian làm việc (trừ giờ nghỉ).
/// Giờ nghỉ của ca qua đêm có thể sau 0:00 (VD ca 22:00-06:00, nghỉ 02:00-02:30).
/// </summary>
public class ShiftBreak
{
    public int Id { get; set; }
    public int ShiftNumber { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

/// <summary>
/// Giờ ca + giờ nghỉ ĐÃ DÙNG cho 1 ngày sản xuất (JSON). Xem lại ngày cũ dùng đúng cài đặt của ngày đó.
/// Tạo khi ngày sản xuất bắt đầu được dùng (Home / chạy nền), cập nhật khi lưu cài đặt ca trong ngày đó.
/// </summary>
public class ShiftDaySnapshot
{
    public int Id { get; set; }
    public DateTime ProductionDate { get; set; }
    public string ShiftsJson { get; set; } = "[]";
    public DateTime UpdatedAt { get; set; }
}
