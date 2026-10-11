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
    /// Xóa kế hoạch của ngày => ngày đó coi như chưa có kế hoạch.
    /// </summary>
    public async Task DeletePlansAsync(DateTime date) => await SavePlansAsync(date, Array.Empty<ProductionPlan>());

    /// <summary>
    /// Kế hoạch trong khoảng ngày [from, to] (dùng cho Export).
    /// </summary>
    public async Task<List<ProductionPlan>> GetPlansInRangeAsync(DateTime from, DateTime to)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var start = from.Date;
        var end = to.Date.AddDays(1);
        return await db.ProductionPlans.AsNoTracking()
            .Where(p => p.PlanDate >= start && p.PlanDate < end)
            .OrderBy(p => p.PlanDate).ThenBy(p => p.GroupName).ThenBy(p => p.ShiftNumber)
            .ToListAsync();
    }

    /// <summary>
    /// Ghi đè kế hoạch của nhiều ngày trong 1 lần (dùng cho Import). Ngày không có trong danh sách giữ nguyên.
    /// </summary>
    public async Task SaveDaysAsync(IReadOnlyDictionary<DateTime, List<ProductionPlan>> plansByDate)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        using var transaction = await db.Database.BeginTransactionAsync();

        foreach (var (date, plans) in plansByDate)
        {
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
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
