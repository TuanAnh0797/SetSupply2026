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
}
