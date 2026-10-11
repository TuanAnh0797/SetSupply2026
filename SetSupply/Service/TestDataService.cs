using APP.Service;
using SetSupply.Models.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SetSupply.Service;

/// <summary>
/// Tạo dữ liệu sản lượng TEST (đánh dấu IsTest, serial dạng "T000123" để không trùng serial thật).
/// </summary>
public class TestDataService
{
    public const int MaxRowsPerRun = 200_000;

    private readonly ProductionDataService _productionService;
    private int _lastSerial = -1;

    public TestDataService(ProductionDataService productionService)
    {
        _productionService = productionService;
    }

    /// <summary>
    /// Sinh dữ liệu cho các ngày sản xuất [fromDate, toDate]: mỗi giờ, mỗi model ngẫu nhiên minPerHour..maxPerHour sản phẩm.
    /// onlyUntilNow = true: không tạo dữ liệu ở thời điểm tương lai.
    /// </summary>
    public async Task<int> GenerateAsync(DateTime fromDate, DateTime toDate, IReadOnlyList<string> models,
                                         int minPerHour, int maxPerHour, bool onlyUntilNow)
    {
        if (models.Count == 0) throw new ArgumentException("Chưa có model nào.");
        if (minPerHour < 0 || maxPerHour < minPerHour) throw new ArgumentException("Số lượng mỗi giờ không hợp lệ (0 <= min <= max).");

        var random = new Random();
        var now = DateTime.Now;
        var rows = new List<ProductionData>();

        for (var day = fromDate.Date; day <= toDate.Date; day = day.AddDays(1))
        {
            var (start, end) = await _productionService.GetProductionRangeAsync(day, null);
            for (var hour = start; hour < end; hour = hour.AddHours(1))
            {
                if (onlyUntilNow && hour >= now) break;
                foreach (var model in models)
                {
                    int count = random.Next(minPerHour, maxPerHour + 1);
                    for (int i = 0; i < count; i++)
                    {
                        var time = hour.AddSeconds(random.Next(0, 3600));
                        if (time >= end || (onlyUntilNow && time > now)) continue;
                        rows.Add(NewRow(model, await NextSerialAsync(), time));
                    }
                    if (rows.Count > MaxRowsPerRun)
                        throw new InvalidOperationException($"Quá nhiều dữ liệu (> {MaxRowsPerRun:#,0} bản ghi). Giảm khoảng ngày / số model / số lượng mỗi giờ.");
                }
            }
        }

        return rows.Count == 0 ? 0 : await _productionService.InsertTestDataAsync(rows);
    }

    /// <summary>
    /// Mã giả lập giống mã PLC: 12 ký tự model + serial test 7 ký tự.
    /// </summary>
    public async Task<string> NewCodeAsync(string model) => FormatCode(model, await NextSerialAsync());

    public static string FormatCode(string model, string serial) => model.Trim().PadRight(PLCService.ModelLength) + serial;

    private static ProductionData NewRow(string model, string serial, DateTime time) => new()
    {
        Codeback = FormatCode(model, serial),
        Model = model.Trim(),
        Serial = serial,
        DateTimeInsert = time,
        IsTest = true
    };

    private async Task<string> NextSerialAsync()
    {
        if (_lastSerial < 0)
            _lastSerial = await _productionService.GetMaxTestSerialNumberAsync();
        _lastSerial++;
        return "T" + _lastSerial.ToString("D6");
    }

    /// <summary>
    /// Sau khi xóa dữ liệu test: đọc lại số serial lớn nhất từ database.
    /// </summary>
    public void ResetSerial() => _lastSerial = -1;
}
