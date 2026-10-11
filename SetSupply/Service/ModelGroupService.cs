using APP.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SetSupply.Models.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SetSupply.Service;

/// <summary>
/// Nhóm model: gán từng model vào nhóm (bảng ModelMappings). Model chưa gán => nhóm "Khác".
/// Dùng DbContext riêng cho mỗi lần gọi (tránh xung đột với DbContext singleton dùng chung).
/// </summary>
public class ModelGroupService
{
    public const string OtherGroupName = "Khác";

    private readonly IServiceScopeFactory _scopeFactory;

    public ModelGroupService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<List<ModelMapping>> GetMappingsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.ModelMappings.AsNoTracking()
            .OrderBy(m => m.SortOrder).ThenBy(m => m.GroupName).ThenBy(m => m.ModelName)
            .ToListAsync();
    }

    /// <summary>
    /// Danh sách tên nhóm theo thứ tự hiển thị; nhóm "Khác" luôn ở cuối.
    /// </summary>
    public async Task<List<string>> GetGroupNamesAsync() => GetGroupNames(await GetMappingsAsync());

    public static List<string> GetGroupNames(IEnumerable<ModelMapping> mappings)
    {
        var names = mappings
            .GroupBy(m => m.GroupName, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Min(m => m.SortOrder)).ThenBy(g => g.Key)
            .Select(g => g.First().GroupName)
            .Where(n => !IsOther(n))
            .ToList();
        names.Add(OtherGroupName);
        return names;
    }

    /// <summary>
    /// Ghi đè toàn bộ bảng gán model.
    /// </summary>
    public async Task SaveMappingsAsync(IEnumerable<ModelMapping> mappings)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        using var transaction = await db.Database.BeginTransactionAsync();

        db.ModelMappings.RemoveRange(await db.ModelMappings.ToListAsync());
        await db.SaveChangesAsync();

        db.ModelMappings.AddRange(mappings.Select(m => new ModelMapping
        {
            GroupName = m.GroupName.Trim(),
            ModelName = m.ModelName.Trim(),
            ImagePath = m.ImagePath?.Trim() ?? string.Empty,
            SortOrder = m.SortOrder
        }));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    /// <summary>
    /// Danh sách model đã sản xuất trong N ngày gần nhất.
    /// </summary>
    public async Task<List<string>> GetRecentModelsAsync(int days = 30)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var from = DateTime.Today.AddDays(-days);
        return await db.ProductionData.AsNoTracking()
            .Where(p => p.DateTimeInsert >= from)
            .Select(p => p.Model)
            .Distinct()
            .OrderBy(m => m)
            .ToListAsync();
    }

    /// <summary>
    /// Bảng tra model -> dòng gán (không phân biệt hoa/thường).
    /// </summary>
    public static Dictionary<string, ModelMapping> ToLookup(IEnumerable<ModelMapping> mappings)
    {
        var lookup = new Dictionary<string, ModelMapping>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in mappings)
            lookup.TryAdd(m.ModelName.Trim(), m);
        return lookup;
    }

    /// <summary>
    /// Nhóm của model; chưa gán => "Khác".
    /// </summary>
    public static string ResolveGroup(string model, IReadOnlyDictionary<string, ModelMapping> lookup)
    {
        return lookup.TryGetValue(model.Trim(), out var mapping) && !string.IsNullOrWhiteSpace(mapping.GroupName) && !IsOther(mapping.GroupName)
            ? mapping.GroupName
            : OtherGroupName;
    }

    public static bool IsOther(string groupName) =>
        string.Equals(groupName.Trim(), OtherGroupName, StringComparison.OrdinalIgnoreCase);
}
