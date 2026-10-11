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
/// Kế hoạch sản lượng theo ngày (ghi đè kế hoạch mặc định của nhóm model).
/// </summary>
public class ProductionPlanService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ProductionPlanService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<List<ProductionPlan>> GetPlansAsync(DateTime date)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var day = date.Date;
        var next = day.AddDays(1);
        return await db.ProductionPlans.AsNoTracking()
            .Where(p => p.PlanDate >= day && p.PlanDate < next)
            .ToListAsync();
    }

    /// <summary>
    /// Ngày gần nhất trước <paramref name="date"/> có kế hoạch riêng (dùng cho "Copy từ ngày trước").
    /// </summary>
    public async Task<DateTime?> GetPreviousPlanDateAsync(DateTime date)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var day = date.Date;
        var latest = await db.ProductionPlans.AsNoTracking()
            .Where(p => p.PlanDate < day)
            .OrderByDescending(p => p.PlanDate)
            .Select(p => (DateTime?)p.PlanDate)
            .FirstOrDefaultAsync();
        return latest?.Date;
    }

    /// <summary>
    /// Ghi đè toàn bộ kế hoạch của 1 ngày.
    /// </summary>
    public async Task SavePlansAsync(DateTime date, IEnumerable<ProductionPlan> plans)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var day = date.Date;
        var next = day.AddDays(1);

        db.ProductionPlans.RemoveRange(await db.ProductionPlans.Where(p => p.PlanDate >= day && p.PlanDate < next).ToListAsync());
        db.ProductionPlans.AddRange(plans.Select(p => new ProductionPlan
        {
            PlanDate = day,
            GroupName = p.GroupName,
            ShiftNumber = p.ShiftNumber,
            Quantity = p.Quantity
        }));
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Xóa kế hoạch riêng của ngày => quay về kế hoạch mặc định.
    /// </summary>
    public async Task DeletePlansAsync(DateTime date) => await SavePlansAsync(date, Array.Empty<ProductionPlan>());
}
