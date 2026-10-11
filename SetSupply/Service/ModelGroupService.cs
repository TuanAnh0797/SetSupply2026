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
/// Quản lý nhóm model + kế hoạch sản lượng.
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

    public async Task<List<ModelGroup>> GetAllAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.ModelGroups.AsNoTracking()
            .OrderBy(g => g.SortOrder).ThenBy(g => g.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Ghi đè toàn bộ danh sách nhóm.
    /// </summary>
    public async Task SaveAllAsync(IEnumerable<ModelGroup> groups)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.ModelGroups.RemoveRange(await db.ModelGroups.ToListAsync());
        db.ModelGroups.AddRange(groups.Select(g => new ModelGroup
        {
            Name = g.Name.Trim(),
            Prefixes = g.Prefixes?.Trim() ?? string.Empty,
            PlanShift1 = g.PlanShift1,
            PlanShift2 = g.PlanShift2,
            PlanShift3 = g.PlanShift3,
            SortOrder = g.SortOrder
        }));
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Danh sách model đã sản xuất trong N ngày gần nhất (dùng để tự tạo nhóm).
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
    /// Tìm nhóm cho model: ưu tiên tiền tố dài nhất, không khớp => nhóm "Khác" (Prefixes rỗng).
    /// </summary>
    public static ModelGroup? Match(string model, IReadOnlyList<ModelGroup> groups)
    {
        ModelGroup? best = null;
        int bestLength = -1;

        foreach (var group in groups)
        {
            foreach (var prefix in group.GetPrefixes())
            {
                if (prefix.Length > bestLength && model.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    best = group;
                    bestLength = prefix.Length;
                }
            }
        }

        return best ?? groups.FirstOrDefault(g => g.IsCatchAll);
    }
}
