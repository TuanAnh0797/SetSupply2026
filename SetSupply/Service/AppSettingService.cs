using APP.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SetSupply.Models.Database;
using System;
using System.Threading.Tasks;

namespace SetSupply.Service;

/// <summary>
/// Cài đặt chung của ứng dụng (bảng AppSettings).
/// </summary>
public class AppSettingService
{
    public const string ApplicationTitleKey = "ApplicationTitle";
    public const string DefaultApplicationTitle = "SetSupply Monitor";

    // Tự động import file kế hoạch
    public const string PlanImportFolderKey = "PlanImport.Folder";
    public const string PlanImportEnabledKey = "PlanImport.Enabled";
    /// <summary>Ca của app cho cột S.1,S.2,S.3 của file kế hoạch, VD "1,2,3" hoặc "1,0,2" (0 = bỏ qua).</summary>
    public const string PlanShiftMapKey = "PlanImport.ShiftMap";
    /// <summary>Tên sheet cần đọc trong file kế hoạch; rỗng = tự dò.</summary>
    public const string PlanSheetNameKey = "PlanImport.SheetName";

    /// <summary>Chế độ bảng Home: "group" (theo nhóm model, mặc định) hoặc "model" (danh sách model + STT).</summary>
    public const string HomeViewModeKey = "Home.ViewMode";

    /// <summary>Số phút mỗi khoảng của biểu đồ sản lượng trên Home (5/10/15/30/60 hoặc tùy chỉnh), mặc định 60.</summary>
    public const string HomeChartIntervalKey = "Home.ChartInterval";

    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>Phát khi 1 cài đặt được lưu. Tham số: key, value.</summary>
    public event Action<string, string>? SettingChanged;

    public AppSettingService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public string Get(string key, string defaultValue)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var value = db.AppSettings.AsNoTracking().FirstOrDefault(s => s.Key == key)?.Value;
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
        }
        catch
        {
            return defaultValue;
        }
    }

    public async Task SetAsync(string key, string value)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var setting = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
            db.AppSettings.Add(new AppSetting { Key = key, Value = value });
        else
            setting.Value = value;
        await db.SaveChangesAsync();
        SettingChanged?.Invoke(key, value);
    }

    public string ApplicationTitle => Get(ApplicationTitleKey, DefaultApplicationTitle);

    public string PlanImportFolder => Get(PlanImportFolderKey, string.Empty);

    public bool PlanImportEnabled => Get(PlanImportEnabledKey, "0") == "1";

    /// <summary>
    /// Tên sheet đọc trong file kế hoạch. Chưa cài => "Daily revise (3S)"; lưu chuỗi "*" => tự dò sheet.
    /// </summary>
    public string PlanSheetName
    {
        get
        {
            var value = Get(PlanSheetNameKey, PlanFileParser.DefaultSheetName).Trim();
            return value == "*" ? string.Empty : value;
        }
    }

    /// <summary>
    /// [ca cho S.1, ca cho S.2, ca cho S.3]; mặc định 1,2,3.
    /// </summary>
    public int[] PlanShiftMap
    {
        get
        {
            var parts = Get(PlanShiftMapKey, "1,2,3").Split(',');
            var map = new[] { 1, 2, 3 };
            for (int i = 0; i < 3 && i < parts.Length; i++)
                if (int.TryParse(parts[i].Trim(), out var v) && v >= 0 && v <= 3) map[i] = v;
            return map;
        }
    }
}
