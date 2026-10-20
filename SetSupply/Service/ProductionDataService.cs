using APP.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SetSupply.Models.Database;
using SetSupply.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SetSupply.Service;

public class ProductionDataService
{
    private readonly AppDbContext _db;
    private readonly ShiftSettingService _shiftService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ShiftScheduleService _scheduleService;
    private List<ShiftSetting> _cachedShiftSettings;

    public ProductionDataService(AppDbContext db, ShiftSettingService shiftService, IServiceScopeFactory scopeFactory,
                                 ShiftScheduleService scheduleService)
    {
        _db = db;
        _shiftService = shiftService;
        _scopeFactory = scopeFactory;
        _scheduleService = scheduleService;
    }

    private async Task<List<ShiftSetting>> GetShiftSettingsAsync()
    {
        if (_cachedShiftSettings == null || !_cachedShiftSettings.Any())
            _cachedShiftSettings = await _shiftService.GetAllShiftSettingsAsync();
        return _cachedShiftSettings;
    }

    public async Task ReloadShiftSettingsAsync()
    {
        _cachedShiftSettings = await _shiftService.GetAllShiftSettingsAsync();
    }

    // ===== CORE: TÍNH KHOẢNG THỜI GIAN THỰC TẾ =====

    /// <summary>
    /// Lấy khoảng DateTime thực tế của 1 ca trong 1 ngày.
    /// VD: ngày 10/03, ca 3 (22:00-06:00) => start=10/03 22:00, end=11/03 06:00
    /// </summary>
    private static (DateTime start, DateTime end) GetShiftDateTimeRange(DateTime date, ShiftSetting shift)
    {
        var start = date.Date + shift.StartTime;
        var end = shift.IsOvernight
            ? date.Date.AddDays(1) + shift.EndTime
            : date.Date + shift.EndTime;
        return (start, end);
    }

    /// <summary>
    /// Lấy khoảng DateTime bao trùm TẤT CẢ ca trong 1 ngày.
    /// VD: Ca 1 (06:00-14:00), Ca 2 (14:00-22:00), Ca 3 (22:00-06:00)
    ///     => start = ngày đó 06:00, end = ngày sau 06:00
    /// </summary>
    private async Task<(DateTime start, DateTime end)> GetFullDayRangeAsync(DateTime date)
    {
        var shifts = await GetShiftSettingsAsync();
        var activeShifts = shifts.Where(s => s.IsUse).OrderBy(s => s.ShiftNumber).ToList();

        if (!activeShifts.Any())
            return (date.Date, date.Date.AddDays(1));

        // Tìm start sớm nhất và end muộn nhất
        DateTime earliest = date.Date.AddDays(1); // max
        DateTime latest = date.Date;               // min

        foreach (var shift in activeShifts)
        {
            var (s, e) = GetShiftDateTimeRange(date, shift);
            if (s < earliest) earliest = s;
            if (e > latest) latest = e;
        }

        return (earliest, latest);
    }

    // ===== KHOẢNG THỜI GIAN CHO MÀN HÌNH HOME (DbContext riêng, luôn đọc cài đặt ca mới nhất) =====

    /// <summary>
    /// Khoảng thời gian thực tế của 1 ngày sản xuất (shiftNumber = null) hoặc 1 ca.
    /// </summary>
    public async Task<(DateTime start, DateTime end)> GetProductionRangeAsync(DateTime date, int? shiftNumber)
    {
        var shifts = await GetShiftsForDateAsync(date);

        if (shiftNumber.HasValue)
        {
            var shift = shifts.FirstOrDefault(s => s.ShiftNumber == shiftNumber.Value);
            if (shift != null)
                return GetShiftDateTimeRange(date, shift);
        }

        if (!shifts.Any())
            return (date.Date, date.Date.AddDays(1));

        var ranges = shifts.Select(s => GetShiftDateTimeRange(date, s)).ToList();
        return (ranges.Min(r => r.start), ranges.Max(r => r.end));
    }

    /// <summary>
    /// Ngày sản xuất chứa thời điểm now. VD: ca đầu bắt đầu 06:00 => 03:00 ngày 11 thuộc ngày sản xuất 10.
    /// </summary>
    public async Task<DateTime> GetProductionDateAsync(DateTime now)
    {
        var (start, _) = await GetProductionRangeAsync(now.Date, null);
        return now < start ? now.Date.AddDays(-1) : now.Date;
    }

    /// <summary>
    /// Các ca đang dùng của 1 ngày sản xuất theo đúng cài đặt của ngày đó (xem ShiftScheduleService).
    /// </summary>
    public async Task<List<ShiftSetting>> GetShiftsForDateAsync(DateTime date)
    {
        var shifts = await _scheduleService.GetForDateAsync(date);
        return shifts.Select(s => new ShiftSetting
        {
            ShiftNumber = s.ShiftNumber,
            StartTime = s.Start,
            EndTime = s.End,
            IsUse = s.IsUse
        }).ToList();
    }

    /// <summary>
    /// Bản ghi mới nhất (hiển thị Codeback khi mở chương trình, trước khi đọc được mã mới).
    /// </summary>
    public async Task<ProductionData?> GetLatestRecordAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.ProductionData.AsNoTracking()
            .OrderByDescending(p => p.DateTimeInsert)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// (Model, thời điểm) của các bản ghi trong khoảng [start, end).
    /// </summary>
    public async Task<List<(string Model, DateTime Time)>> GetModelTimesInRangeAsync(DateTime start, DateTime end)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rows = await db.ProductionData.AsNoTracking()
            .Where(p => p.DateTimeInsert >= start && p.DateTimeInsert < end)
            .Select(p => new { p.Model, p.DateTimeInsert })
            .ToListAsync();
        return rows.Select(r => (r.Model, r.DateTimeInsert)).ToList();
    }

    // ===== THÊM DỮ LIỆU =====

    public async Task<bool> AddProductionDataAsync(string codeback, string model, string serial)
    {
        try
        {
            return await TryInsertProductionAsync(codeback, model, serial);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error adding production data: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Lưu 1 sản phẩm đọc từ PLC. true = đã lưu, false = trùng (cùng model + serial).
    /// Lỗi database sẽ ném exception để bên gọi KHÔNG xác nhận (reset trigger) với PLC.
    /// </summary>
    public async Task<bool> TryInsertProductionAsync(string codeback, string model, string serial, bool isTest = false)
    {
        // DbContext riêng: hàm này chạy trên thread PLC, song song với các truy vấn khác
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Trùng = cùng model + cùng serial (2 model khác nhau có thể trùng số serial)
        if (await db.ProductionData.AnyAsync(p => p.Model == model && p.Serial == serial))
            return false;

        db.ProductionData.Add(new ProductionData
        {
            Codeback = codeback,
            Model = model,
            Serial = serial,
            DateTimeInsert = DateTime.Now,
            IsTest = isTest
        });
        await db.SaveChangesAsync();
        return true;
    }

    // ===== DỮ LIỆU TEST (Cài đặt > Test Data) =====

    /// <summary>Phát sau khi thêm / xóa dữ liệu test hàng loạt (màn hình Home tải lại).</summary>
    public event Action? DataChanged;

    /// <summary>
    /// Thêm hàng loạt bản ghi test (đã đánh dấu IsTest).
    /// </summary>
    public async Task<int> InsertTestDataAsync(IReadOnlyCollection<ProductionData> rows)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        foreach (var row in rows) row.IsTest = true;
        db.ProductionData.AddRange(rows);
        await db.SaveChangesAsync();
        DataChanged?.Invoke();
        return rows.Count;
    }

    /// <summary>
    /// Số bản ghi test; from/to = khoảng thời gian (null = tất cả).
    /// </summary>
    public async Task<int> CountTestDataAsync(DateTime? from = null, DateTime? to = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var query = db.ProductionData.Where(p => p.IsTest);
        if (from.HasValue) query = query.Where(p => p.DateTimeInsert >= from.Value);
        if (to.HasValue) query = query.Where(p => p.DateTimeInsert < to.Value);
        return await query.CountAsync();
    }

    /// <summary>
    /// Xóa CHỈ bản ghi test; from/to = khoảng thời gian (null = tất cả). Trả về số bản ghi đã xóa.
    /// </summary>
    public async Task<int> DeleteTestDataAsync(DateTime? from = null, DateTime? to = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        int deleted;
        if (from.HasValue && to.HasValue)
            deleted = await db.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM \"ProductionData\" WHERE \"IsTest\" = 1 AND \"DateTimeInsert\" >= {from.Value} AND \"DateTimeInsert\" < {to.Value}");
        else
            deleted = await db.Database.ExecuteSqlRawAsync("DELETE FROM \"ProductionData\" WHERE \"IsTest\" = 1");
        DataChanged?.Invoke();
        return deleted;
    }

    /// <summary>
    /// Serial test lớn nhất đang có (serial dạng "T" + số) để sinh serial mới không trùng.
    /// </summary>
    public async Task<int> GetMaxTestSerialNumberAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var serials = await db.ProductionData.AsNoTracking()
            .Where(p => p.IsTest && p.Serial.StartsWith("T"))
            .Select(p => p.Serial)
            .ToListAsync();
        return serials.Select(s => int.TryParse(s.Substring(1), out var n) ? n : 0).DefaultIfEmpty(0).Max();
    }

    public async Task<int> AddBatchProductionDataAsync(List<ProductionData> dataList)
    {
        try
        {
            int addedCount = 0;
            foreach (var data in dataList)
            {
                if (!await _db.ProductionData.AnyAsync(p => p.Serial == data.Serial))
                {
                    _db.ProductionData.Add(data);
                    addedCount++;
                }
            }
            await _db.SaveChangesAsync();
            return addedCount;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error adding batch: {ex.Message}");
            return 0;
        }
    }

    // ===== QUERY THEO NGÀY (BAO GỒM CA QUA ĐÊM) =====

    /// <summary>
    /// Lấy toàn bộ data của 1 ngày — bao gồm phần ca qua đêm sang ngày hôm sau.
    /// VD: ngày 10/03 có ca 3 (22:00-06:00) => lấy cả data 00:00-06:00 ngày 11/03.
    /// </summary>
    public async Task<List<ProductionData>> GetProductionByDateAsync(DateTime date)
    {
        var (start, end) = await GetFullDayRangeAsync(date);

        return await _db.ProductionData
            .Where(p => p.DateTimeInsert >= start && p.DateTimeInsert < end)
            .OrderBy(p => p.DateTimeInsert)
            .ToListAsync();
    }

    public async Task<List<ProductionData>> GetProductionByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        // Lấy start của ngày đầu, end của ngày cuối (bao gồm ca qua đêm)
        var (start, _) = await GetFullDayRangeAsync(fromDate);
        var (_, end) = await GetFullDayRangeAsync(toDate);

        return await _db.ProductionData
            .Where(p => p.DateTimeInsert >= start && p.DateTimeInsert < end)
            .OrderBy(p => p.DateTimeInsert)
            .ToListAsync();
    }

    public async Task<List<ProductionData>> GetProductionByHourAsync(DateTime date, int hour)
    {
        return await _db.ProductionData
            .Where(p => p.DateTimeInsert.Date == date.Date && p.DateTimeInsert.Hour == hour)
            .OrderBy(p => p.DateTimeInsert)
            .ToListAsync();
    }

    // ===== QUERY THEO CA (HỖ TRỢ CA QUA ĐÊM) =====

    /// <summary>
    /// Lấy dữ liệu theo ngày + ca — query theo khoảng DateTime thực tế.
    /// Ca qua đêm sẽ lấy đúng data sang ngày hôm sau.
    /// </summary>
    public async Task<List<ProductionData>> GetProductionByDateAndShiftAsync(DateTime date, int shiftNumber)
    {
        var shiftSettings = await GetShiftSettingsAsync();
        var shift = shiftSettings.FirstOrDefault(s => s.ShiftNumber == shiftNumber);

        if (shift == null)
            return new List<ProductionData>();

        var (start, end) = GetShiftDateTimeRange(date, shift);

        return await _db.ProductionData
            .Where(p => p.DateTimeInsert >= start && p.DateTimeInsert < end)
            .OrderBy(p => p.DateTimeInsert)
            .ToListAsync();
    }

    public async Task<List<ProductionData>> GetProductionByDateShiftModelAsync(DateTime date, int shiftNumber, string model)
    {
        var data = await GetProductionByDateAndShiftAsync(date, shiftNumber);
        return data.Where(p => p.Model == model).ToList();
    }

    // ===== ĐẾM SẢN LƯỢNG =====

    public async Task<int> CountProductionByHourAndModelAsync(DateTime date, int hour, string model)
    {
        return await _db.ProductionData
            .Where(p => p.DateTimeInsert.Date == date.Date
                     && p.DateTimeInsert.Hour == hour
                     && p.Model == model)
            .CountAsync();
    }

    public async Task<int> GetTotalProductionByDateAsync(DateTime date)
    {
        var data = await GetProductionByDateAsync(date);
        return data.Count;
    }

    public async Task<int> GetTotalProductionByDateAndShiftAsync(DateTime date, int shiftNumber)
    {
        var data = await GetProductionByDateAndShiftAsync(date, shiftNumber);
        return data.Count;
    }

    public async Task<int> GetTotalProductionByDateShiftModelAsync(DateTime date, int shiftNumber, string model)
    {
        var data = await GetProductionByDateAndShiftAsync(date, shiftNumber);
        return data.Count(p => p.Model == model);
    }

    // ===== DANH SÁCH MODEL =====

    public async Task<List<string>> GetModelsForDateAsync(DateTime date)
    {
        var data = await GetProductionByDateAsync(date);
        return data.Select(p => p.Model).Distinct().OrderBy(m => m).ToList();
    }

    public async Task<List<string>> GetModelsForDateAndShiftAsync(DateTime date, int shiftNumber)
    {
        var data = await GetProductionByDateAndShiftAsync(date, shiftNumber);
        return data.Select(p => p.Model).Distinct().OrderBy(m => m).ToList();
    }

    // ===== MA TRẬN DỮ LIỆU CHO DATAGRID =====

    /// <summary>
    /// Ma trận: Model -> Hour -> Count. Hỗ trợ ca qua đêm.
    /// </summary>
    public async Task<Dictionary<string, Dictionary<int, int>>> GetProductionMatrixAsync(DateTime date, int shiftNumber)
    {
        var data = await GetProductionByDateAndShiftAsync(date, shiftNumber);
        return BuildMatrix(data);
    }

    /// <summary>
    /// Ma trận cả ngày (tất cả ca). Hỗ trợ ca qua đêm.
    /// </summary>
    public async Task<Dictionary<string, Dictionary<int, int>>> GetProductionMatrixForDateAsync(DateTime date)
    {
        var data = await GetProductionByDateAsync(date);
        return BuildMatrix(data);
    }

    private static Dictionary<string, Dictionary<int, int>> BuildMatrix(List<ProductionData> data)
    {
        var matrix = new Dictionary<string, Dictionary<int, int>>();

        foreach (var item in data)
        {
            if (!matrix.ContainsKey(item.Model))
                matrix[item.Model] = new Dictionary<int, int>();

            if (!matrix[item.Model].ContainsKey(item.Hour))
                matrix[item.Model][item.Hour] = 0;

            matrix[item.Model][item.Hour]++;
        }

        return matrix;
    }

    // ===== THỐNG KÊ =====

    public async Task<Dictionary<string, int>> GetProductionStatsByModelAsync(DateTime date)
    {
        var data = await GetProductionByDateAsync(date);
        return data.GroupBy(p => p.Model).ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Thống kê theo giờ — bao gồm ca qua đêm.
    /// </summary>
    public async Task<Dictionary<int, int>> GetProductionStatsByHourAsync(DateTime date)
    {
        var data = await GetProductionByDateAsync(date);
        return data.GroupBy(p => p.Hour).ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Thống kê theo ca — hỗ trợ ca qua đêm.
    /// </summary>
    public async Task<Dictionary<int, int>> GetProductionStatsByShiftAsync(DateTime date)
    {
        var shiftSettings = await GetShiftSettingsAsync();
        var stats = new Dictionary<int, int>();

        foreach (var shift in shiftSettings.Where(s => s.IsUse))
        {
            var data = await GetProductionByDateAndShiftAsync(date, shift.ShiftNumber);
            stats[shift.ShiftNumber] = data.Count;
        }

        return stats;
    }

    // ===== TÌM KIẾM =====

    public async Task<bool> IsSerialExistsAsync(string serial)
    {
        return await _db.ProductionData.AnyAsync(p => p.Serial == serial);
    }

    public async Task<ProductionData?> GetProductionBySerialAsync(string serial)
    {
        return await _db.ProductionData.FirstOrDefaultAsync(p => p.Serial == serial);
    }

    public async Task<List<ProductionData>> SearchByModelAsync(string model, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _db.ProductionData.Where(p => p.Model.Contains(model));
        if (fromDate.HasValue) query = query.Where(p => p.DateTimeInsert >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(p => p.DateTimeInsert < toDate.Value.Date.AddDays(1));
        return await query.OrderBy(p => p.DateTimeInsert).ToListAsync();
    }

    public async Task<List<ProductionData>> SearchByCodebackAsync(string codeback, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _db.ProductionData.Where(p => p.Codeback.Contains(codeback));
        if (fromDate.HasValue) query = query.Where(p => p.DateTimeInsert >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(p => p.DateTimeInsert < toDate.Value.Date.AddDays(1));
        return await query.OrderBy(p => p.DateTimeInsert).ToListAsync();
    }

    // ===== XÓA DỮ LIỆU =====

    public async Task<int> DeleteOldDataAsync(int daysToKeep = 30)
    {
        try
        {
            var cutoff = DateTime.Now.AddDays(-daysToKeep);
            var old = await _db.ProductionData.Where(p => p.DateTimeInsert < cutoff).ToListAsync();

            if (old.Any())
            {
                _db.ProductionData.RemoveRange(old);
                await _db.SaveChangesAsync();
                return old.Count;
            }
            return 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting old data: {ex.Message}");
            return 0;
        }
    }

    public async Task<bool> DeleteProductionByDateAsync(DateTime date)
    {
        try
        {
            var data = await GetProductionByDateAsync(date);
            if (data.Any())
            {
                _db.ProductionData.RemoveRange(data);
                await _db.SaveChangesAsync();
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting by date: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteBySerialAsync(string serial)
    {
        try
        {
            var data = await _db.ProductionData.FirstOrDefaultAsync(p => p.Serial == serial);
            if (data != null)
            {
                _db.ProductionData.Remove(data);
                await _db.SaveChangesAsync();
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting by serial: {ex.Message}");
            return false;
        }
    }

    // ===== UTILITY =====

    public async Task<int> GetTotalRecordsAsync()
    {
        return await _db.ProductionData.CountAsync();
    }

    public async Task<ProductionData?> GetLatestProductionAsync()
    {
        return await _db.ProductionData.OrderByDescending(p => p.DateTimeInsert).FirstOrDefaultAsync();
    }

    public async Task<ProductionData?> GetOldestProductionAsync()
    {
        return await _db.ProductionData.OrderBy(p => p.DateTimeInsert).FirstOrDefaultAsync();
    }

    public async Task<bool> HasDataForDateAsync(DateTime date)
    {
        var (start, end) = await GetFullDayRangeAsync(date);
        return await _db.ProductionData.AnyAsync(p => p.DateTimeInsert >= start && p.DateTimeInsert < end);
    }
}