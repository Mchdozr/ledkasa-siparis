using LedKasa.Siparis.Common;
using LedKasa.Siparis.Features.Orders;
using LedKasa.Siparis.Features.Orders.Domain;
using LedKasa.Siparis.Features.Reports;
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

internal sealed class DashboardService : IDashboardService
{
    private readonly IReportingDbContext _db;

    public DashboardService(IReportingDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        var today = TurkeyTime.Today;
        var orders = _db.Orders.AsQueryable();

        var todayCount = await OrderScopes.Apply(orders, OrderListScope.Today, today).CountAsync(cancellationToken);
        var openCount = await OrderScopes.Apply(orders, OrderListScope.Open, today).CountAsync(cancellationToken);
        var weekDeliveryCount = await OrderScopes.Apply(orders, OrderListScope.WeekDelivery, today).CountAsync(cancellationToken);
        var overdueCount = await OrderScopes.Apply(orders, OrderListScope.Overdue, today).CountAsync(cancellationToken);

        var statusCounts = await _db.Orders
            .GroupBy(o => o.Status)
            .Select(g => new StatusCount(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        var upcoming = await _db.Orders
            .Where(o => o.DeliveryDate >= today && OrderScopes.Open.Contains(o.Status))
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
}
