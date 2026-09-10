using LedKasa.Siparis.Common;
using LedKasa.Siparis.Data;
using LedKasa.Siparis.Features.Orders.Domain;
using Microsoft.EntityFrameworkCore;

namespace LedKasa.Siparis.Features.Dashboard;

public sealed class DashboardSnapshot
{
    public int TodayCount { get; init; }
    public int OpenCount { get; init; }
    public int WeekDeliveryCount { get; init; }
    public int OverdueCount { get; init; }
    public IReadOnlyList<StatusCount> StatusCounts { get; init; } = [];
    public IReadOnlyList<UpcomingDelivery> Upcoming { get; init; } = [];
}

public sealed record StatusCount(OrderStatus Status, int Count);
public sealed record UpcomingDelivery(int Id, string OrderNumber, string CustomerName, DateOnly DeliveryDate, DeliveryPlace Place, OrderStatus Status);

public interface IDashboardService
{
    Task<DashboardSnapshot> GetAsync(CancellationToken cancellationToken = default);
}

public sealed class DashboardService : IDashboardService
{
    private static readonly OrderStatus[] OpenStatuses =
    [
        OrderStatus.Yeni, OrderStatus.Onaylandi, OrderStatus.Uretimde, OrderStatus.Hazir
    ];

    private readonly ApplicationDbContext _db;

    public DashboardService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        var today = TurkeyTime.Today;
        var week = ReportPeriodCalculatorRange(today);

        var todayCount = await _db.Orders.CountAsync(o => o.OrderDate == today, cancellationToken);
        var openCount = await _db.Orders.CountAsync(o => OpenStatuses.Contains(o.Status), cancellationToken);
        var weekDeliveryCount = await _db.Orders.CountAsync(
            o => o.DeliveryDate >= week.From && o.DeliveryDate <= week.To && o.Status != OrderStatus.Iptal,
            cancellationToken);
        var overdueCount = await _db.Orders.CountAsync(
            o => o.DeliveryDate < today && OpenStatuses.Contains(o.Status),
            cancellationToken);

        var statusCounts = await _db.Orders
            .GroupBy(o => o.Status)
            .Select(g => new StatusCount(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        var upcoming = await _db.Orders
            .Where(o => o.DeliveryDate >= today && OpenStatuses.Contains(o.Status))
            .OrderBy(o => o.DeliveryDate)
            .Take(8)
            .Select(o => new UpcomingDelivery(o.Id, o.OrderNumber, o.CustomerName, o.DeliveryDate, o.DeliveryPlace, o.Status))
            .ToListAsync(cancellationToken);

        return new DashboardSnapshot
        {
            TodayCount = todayCount,
            OpenCount = openCount,
            WeekDeliveryCount = weekDeliveryCount,
            OverdueCount = overdueCount,
            StatusCounts = statusCounts,
            Upcoming = upcoming
        };
    }

    private static (DateOnly From, DateOnly To) ReportPeriodCalculatorRange(DateOnly today)
        => Features.Reports.ReportPeriodCalculator.GetRange(Features.Reports.ReportPeriod.Weekly, today);
}
