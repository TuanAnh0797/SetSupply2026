using APP.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SetSupply.Models.Database;
using SetSupply.Models.Home;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SetSupply.Service;

/// <summary>
/// Giờ ca + giờ nghỉ theo từng NGÀY SẢN XUẤT.
/// - Cài đặt hiện tại: bảng ShiftSettings + ShiftBreaks.
/// - Mỗi ngày sản xuất có 1 bản chụp (ShiftDaySnapshots) cài đặt đã dùng cho ngày đó:
///   tạo khi ngày đó được dùng lần đầu (Home / chạy nền), ghi đè khi lưu cài đặt ca trong ngày đó.
/// - Xem ngày cũ: dùng bản chụp; ngày cũ chưa có bản chụp (trước khi có tính năng này) dùng ShiftSettingHistory
///   cho giờ ca + giờ nghỉ hiện tại. Hôm nay / tương lai: cài đặt hiện tại.
/// </summary>
public class ShiftScheduleService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>Phát sau khi lưu cài đặt ca / giờ nghỉ.</summary>
    public event Action? ScheduleChanged;

    public ShiftScheduleService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    // ===== CÀI ĐẶT HIỆN TẠI =====

    /// <summary>Cả 3 ca (kể cả ca không dùng) theo cài đặt hiện tại.</summary>
    public async Task<List<ShiftDefinition>> GetCurrentAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await LoadCurrentAsync(db);
    }

    private static async Task<List<ShiftDefinition>> LoadCurrentAsync(AppDbContext db)
    {
        var shifts = await db.ShiftSettings.AsNoTracking().OrderBy(s => s.ShiftNumber).ToListAsync();
        var breaks = await db.ShiftBreaks.AsNoTracking().OrderBy(b => b.ShiftNumber).ThenBy(b => b.SortOrder).ToListAsync();
        return shifts.Select(s => new ShiftDefinition
        {
            ShiftNumber = s.ShiftNumber,
            Start = s.StartTime,
            End = s.EndTime,
            IsUse = s.IsUse,
            Breaks = breaks.Where(b => b.ShiftNumber == s.ShiftNumber)
                           .Select(b => new BreakDefinition { Start = b.StartTime, End = b.EndTime, Name = b.Name })
                           .ToList()
        }).ToList();
    }

    // ===== THEO NGÀY SẢN XUẤT =====

    /// <summary>
    /// Các ca ĐANG DÙNG của 1 ngày sản xuất (kèm giờ nghỉ), theo đúng cài đặt của ngày đó.
    /// </summary>
    public async Task<List<ShiftDefinition>> GetForDateAsync(DateTime productionDate)
    {
        var all = await GetAllForDateAsync(productionDate);
        return all.Where(s => s.IsUse).OrderBy(s => s.ShiftNumber).ToList();
    }

    /// <summary>Cả 3 ca của 1 ngày sản xuất (kể cả ca không dùng).</summary>
    public async Task<List<ShiftDefinition>> GetAllForDateAsync(DateTime productionDate)
    {
        var day = productionDate.Date;
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var snapshot = await FindSnapshotAsync(db, day);
        if (snapshot != null)
        {
            var fromSnapshot = Deserialize(snapshot.ShiftsJson);
            if (fromSnapshot.Count > 0) return fromSnapshot;
        }

        var current = await LoadCurrentAsync(db);
        var currentProductionDate = GetProductionDate(current, DateTime.Now);

        if (day < currentProductionDate)
        {
            // Ngày cũ chưa có bản chụp: giờ ca theo lịch sử cũ (nếu có), giờ nghỉ theo cài đặt hiện tại
            var next = day.AddDays(1);
            var history = await db.ShiftSettingHistory.AsNoTracking()
                .Where(h => h.DateUsing >= day && h.DateUsing < next)
                .ToListAsync();
            foreach (var h in history.GroupBy(h => h.ShiftNumber).Select(g => g.OrderByDescending(x => x.InsertTime).First()))
            {
                var shift = current.FirstOrDefault(s => s.ShiftNumber == h.ShiftNumber);
                if (shift == null) continue;
                shift.Start = h.StartTime;
                shift.End = h.EndTime;
                shift.IsUse = h.IsUse;
            }
            return current;
        }

        // Ngày sản xuất hiện tại: lưu bản chụp để sau này xem lại đúng cài đặt của ngày này
        if (day == currentProductionDate)
            await UpsertSnapshotAsync(db, day, current);
        return current;
    }

    /// <summary>
    /// Ngày sản xuất chứa thời điểm <paramref name="now"/> theo cài đặt hiện tại
    /// (ngày sản xuất bắt đầu từ giờ bắt đầu sớm nhất của các ca đang dùng).
    /// </summary>
    public async Task<DateTime> GetCurrentProductionDateAsync()
    {
        var current = await GetCurrentAsync();
        return GetProductionDate(current, DateTime.Now);
    }

    public static DateTime GetProductionDate(IEnumerable<ShiftDefinition> shifts, DateTime now)
    {
        var active = shifts.Where(s => s.IsUse).ToList();
        if (active.Count == 0) return now.Date;
        var dayStart = active.Min(s => s.GetRange(now.Date).Start);
        return now < dayStart ? now.Date.AddDays(-1) : now.Date;
    }

    /// <summary>Tạo bản chụp cho ngày sản xuất hiện tại nếu chưa có (chạy nền định kỳ).</summary>
    public async Task EnsureCurrentSnapshotAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var current = await LoadCurrentAsync(db);
        var day = GetProductionDate(current, DateTime.Now);
        if (await FindSnapshotAsync(db, day) == null)
            await UpsertSnapshotAsync(db, day, current);
    }

    // ===== LƯU CÀI ĐẶT =====

    /// <summary>
    /// Kiểm tra cài đặt. Trả về thông báo lỗi hoặc null nếu hợp lệ.
    /// </summary>
    public static string? Validate(IReadOnlyList<ShiftDefinition> shifts)
    {
        if (!shifts.Any(s => s.IsUse))
            return "Cần dùng ít nhất 1 ca.";

        var refDay = new DateTime(2000, 1, 3);
        foreach (var shift in shifts.Where(s => s.IsUse))
        {
            var (shiftStart, shiftEnd) = shift.GetRange(refDay);
            var ranges = new List<(DateTime Start, DateTime End, BreakDefinition Break)>();
            foreach (var b in shift.Breaks)
            {
                if (b.End == b.Start)
                    return $"Ca {shift.ShiftNumber}: giờ nghỉ {b.Start:hh\\:mm}-{b.End:hh\\:mm} có giờ bắt đầu = giờ kết thúc.";
                var bs = refDay + b.Start;
                if (bs < shiftStart) bs = bs.AddDays(1);
                var be = bs.Date + b.End;
                if (be <= bs) be = be.AddDays(1);
                if (bs < shiftStart || be > shiftEnd)
                    return $"Ca {shift.ShiftNumber} ({shift.TimeRangeText}): giờ nghỉ {b.Start:hh\\:mm}-{b.End:hh\\:mm} nằm ngoài giờ ca.";
                ranges.Add((bs, be, b));
            }

            var sorted = ranges.OrderBy(r => r.Start).ToList();
            for (int i = 1; i < sorted.Count; i++)
            {
                if (sorted[i].Start < sorted[i - 1].End)
                    return $"Ca {shift.ShiftNumber}: giờ nghỉ {sorted[i - 1].Break.Start:hh\\:mm}-{sorted[i - 1].Break.End:hh\\:mm} " +
                           $"và {sorted[i].Break.Start:hh\\:mm}-{sorted[i].Break.End:hh\\:mm} bị chồng nhau.";
            }

            if (shift.TotalWorkMinutes(refDay) <= 0)
                return $"Ca {shift.ShiftNumber}: giờ nghỉ chiếm hết thời gian ca.";
        }
        return null;
    }

    /// <summary>
    /// Lưu cài đặt ca + giờ nghỉ. Áp dụng từ ngày sản xuất hiện tại (ghi đè bản chụp của ngày này);
    /// các ngày đã qua giữ nguyên cài đặt cũ.
    /// </summary>
    public async Task SaveAsync(IReadOnlyList<ShiftDefinition> shifts)
    {
        var error = Validate(shifts);
        if (error != null) throw new InvalidOperationException(error);

        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await using var tx = await db.Database.BeginTransactionAsync();

            var settings = await db.ShiftSettings.ToListAsync();
            foreach (var shift in shifts)
            {
                var row = settings.FirstOrDefault(s => s.ShiftNumber == shift.ShiftNumber);
                if (row == null)
                {
                    row = new ShiftSetting { ShiftNumber = shift.ShiftNumber, InsertTime = DateTime.Now };
                    db.ShiftSettings.Add(row);
                }
                row.StartTime = shift.Start;
                row.EndTime = shift.End;
                row.IsUse = shift.IsUse;
                row.UpdateTime = DateTime.Now;
            }

            db.ShiftBreaks.RemoveRange(await db.ShiftBreaks.ToListAsync());
            foreach (var shift in shifts)
            {
                int order = 0;
                foreach (var b in shift.Breaks.OrderBy(b => b.Start < shift.Start ? b.Start + TimeSpan.FromDays(1) : b.Start))
                {
                    db.ShiftBreaks.Add(new ShiftBreak
                    {
                        ShiftNumber = shift.ShiftNumber,
                        StartTime = b.Start,
                        EndTime = b.End,
                        Name = b.Name?.Trim() ?? string.Empty,
                        SortOrder = ++order
                    });
                }
            }
            await db.SaveChangesAsync();

            // Áp dụng cho ngày sản xuất hiện tại (tính theo cài đặt MỚI)
            var day = GetProductionDate(shifts, DateTime.Now);
            await UpsertSnapshotAsync(db, day, shifts);

            // Giữ bảng lịch sử cũ (trang History) cập nhật theo cài đặt mới của ngày này
            var next = day.AddDays(1);
            var history = await db.ShiftSettingHistory.Where(h => h.DateUsing >= day && h.DateUsing < next).ToListAsync();
            foreach (var shift in shifts)
            {
                var h = history.FirstOrDefault(x => x.ShiftNumber == shift.ShiftNumber);
                if (h == null)
                {
                    h = new ShiftSettingHistory { DateUsing = day, ShiftNumber = shift.ShiftNumber };
                    db.ShiftSettingHistory.Add(h);
                }
                h.StartTime = shift.Start;
                h.EndTime = shift.End;
                h.IsUse = shift.IsUse;
                h.InsertTime = DateTime.Now;
            }
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        ScheduleChanged?.Invoke();
    }

    // ===== BẢN CHỤP =====

    private static async Task<ShiftDaySnapshot?> FindSnapshotAsync(AppDbContext db, DateTime day)
    {
        var next = day.AddDays(1);
        return await db.ShiftDaySnapshots
            .Where(s => s.ProductionDate >= day && s.ProductionDate < next)
            .OrderByDescending(s => s.UpdatedAt)
            .FirstOrDefaultAsync();
    }

    private static async Task UpsertSnapshotAsync(AppDbContext db, DateTime day, IEnumerable<ShiftDefinition> shifts)
    {
        var json = Serialize(shifts);
        var existing = await FindSnapshotAsync(db, day);
        if (existing == null)
            db.ShiftDaySnapshots.Add(new ShiftDaySnapshot { ProductionDate = day.Date, ShiftsJson = json, UpdatedAt = DateTime.Now });
        else
        {
            existing.ShiftsJson = json;
            existing.UpdatedAt = DateTime.Now;
        }
        await db.SaveChangesAsync();
    }

    private sealed class BreakDto
    {
        public string Start { get; set; } = "00:00";
        public string End { get; set; } = "00:00";
        public string Name { get; set; } = string.Empty;
    }

    private sealed class ShiftDto
    {
        public int Shift { get; set; }
        public string Start { get; set; } = "00:00";
        public string End { get; set; } = "00:00";
        public bool IsUse { get; set; }
        public List<BreakDto> Breaks { get; set; } = new();
    }

    private static string Serialize(IEnumerable<ShiftDefinition> shifts) =>
        JsonSerializer.Serialize(shifts.Select(s => new ShiftDto
        {
            Shift = s.ShiftNumber,
            Start = FormatTime(s.Start),
            End = FormatTime(s.End),
            IsUse = s.IsUse,
            Breaks = s.Breaks.Select(b => new BreakDto { Start = FormatTime(b.Start), End = FormatTime(b.End), Name = b.Name }).ToList()
        }).ToList(), JsonOptions);

    private static List<ShiftDefinition> Deserialize(string json)
    {
        try
        {
            var dtos = JsonSerializer.Deserialize<List<ShiftDto>>(json) ?? new List<ShiftDto>();
            return dtos.Select(d => new ShiftDefinition
            {
                ShiftNumber = d.Shift,
                Start = ParseTime(d.Start),
                End = ParseTime(d.End),
                IsUse = d.IsUse,
                Breaks = d.Breaks.Select(b => new BreakDefinition { Start = ParseTime(b.Start), End = ParseTime(b.End), Name = b.Name }).ToList()
            }).OrderBy(s => s.ShiftNumber).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Shift snapshot JSON error: {ex.Message}");
            return new List<ShiftDefinition>();
        }
    }

    private static string FormatTime(TimeSpan t) => t.ToString(@"hh\:mm", CultureInfo.InvariantCulture);

    private static readonly string[] ClockFormats = { @"h\:mm", @"hh\:mm" };

    private static TimeSpan ParseTime(string text) =>
        TimeSpan.TryParseExact(text, ClockFormats, CultureInfo.InvariantCulture, out var t) ? t : TimeSpan.Zero;

    /// <summary>
    /// Đọc giờ dạng "H:mm" / "HH:mm" / "HHmm" (giờ nghỉ nhập tay). Trả về null nếu sai.
    /// </summary>
    public static TimeSpan? TryParseClock(string? text)
    {
        text = (text ?? string.Empty).Trim().Replace('.', ':').Replace('h', ':').Replace('H', ':');
        if (text.EndsWith(":")) text += "00";
        if (!text.Contains(':') && text.Length is 3 or 4 && int.TryParse(text, out _))
            text = text[..^2] + ":" + text[^2..];
        if (!text.Contains(':') && int.TryParse(text, out var hourOnly))
            text = hourOnly + ":00";
        if (TimeSpan.TryParseExact(text, ClockFormats, CultureInfo.InvariantCulture, out var t) && t < TimeSpan.FromDays(1))
            return t;
        return null;
    }
}
