using LedKasa.Siparis.Common;
using LedKasa.Siparis.Features.Orders;
using LedKasa.Siparis.Features.Orders.Domain;
using Microsoft.EntityFrameworkCore;

namespace LedKasa.Siparis.Features.Reports;

public interface IReportService
{
    Task<ReportResult> GetAsync(ReportRequest request, CancellationToken cancellationToken = default);
    Task<ReportResult> GetForOrderListAsync(OrderListFilter filter, CancellationToken cancellationToken = default);
}

internal sealed class ReportService : IReportService
{
    private readonly IReportingDbContextFactory _dbFactory;

    public ReportService(IReportingDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<ReportResult> GetAsync(ReportRequest request, CancellationToken cancellationToken = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var (from, to) = ReportPeriodCalculator.GetRange(request.Period, request.Anchor);
        var query = db.Orders.AsNoTracking()
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .AsQueryable();

        query = request.DateField == ReportDateField.DeliveryDate
            ? query.Where(o => o.DeliveryDate >= from && o.DeliveryDate <= to)
            : query.Where(o => o.OrderDate >= from && o.OrderDate <= to);

        if (request.DeliveryPlace.HasValue)
            query = query.Where(o => o.DeliveryPlace == request.DeliveryPlace.Value);

        var statuses = (request.Statuses ?? [])
            .Where(Enum.IsDefined)
            .Distinct()
            .ToArray();
        query = statuses.Length == 0
            ? query.Where(o => o.Status != OrderStatus.Iptal)
            : query.Where(o => statuses.Contains(o.Status));

        if (!string.IsNullOrWhiteSpace(request.CustomerName))
        {
            var term = request.CustomerName.Trim();
            query = query.Where(o => o.CustomerName.Contains(term));
        }

        var orders = await query
            .OrderBy(o => o.OrderDate)
            .ThenBy(o => o.OrderNumber)
            .ToListAsync(cancellationToken);

        return BuildResult(orders, from, to, BuildTitle(request.Period, from, to));
    }

    public async Task<ReportResult> GetForOrderListAsync(OrderListFilter filter, CancellationToken cancellationToken = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var today = TurkeyTime.Today;
        var orders = await db.Orders.AsNoTracking()
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .ApplyFilter(filter, today)
            .ApplySort(filter.Sort)
            .ToListAsync(cancellationToken);

        var dates = orders
            .Select(o => filter.DateField == ReportDateFieldKind.DeliveryDate ? o.DeliveryDate : o.OrderDate)
            .ToList();
        var from = filter.From ?? (dates.Count == 0 ? today : dates.Min());
        var to = filter.To ?? (dates.Count == 0 ? today : dates.Max());

        return BuildResult(orders, from, to, BuildOrderListTitle(filter), includeNotes: true);
    }

    private static ReportResult BuildResult(
        IReadOnlyList<Order> orders, DateOnly from, DateOnly to, string title, bool includeNotes = false)
    {
        var rows = orders.Select(o => new ReportRow
        {
            OrderNumber = o.OrderNumber,
            CustomerName = o.CustomerName,
            OrderDate = o.OrderDate,
            DeliveryDate = o.DeliveryDate,
            DeliveryPlace = DisplayNames.Place(o.DeliveryPlace),
            Status = DisplayNames.Status(o.Status),
            ItemCount = o.Items.Count,
            TotalQuantity = o.Items.Sum(i => i.Quantity),
            ItemSummary = string.Join(" · ", o.Items.Select(i =>
                $"{i.Product?.Name ?? "Ürün"} {i.WidthCm}x{i.HeightCm}x{i.DepthCm} cm {DisplayNames.Side(i.Side)} x{i.Quantity}")),
            Notes = includeNotes ? o.Notes : null
        }).ToList();

        return new ReportResult
        {
            From = from,
            To = to,
            Title = title,
            OrderCount = rows.Count,
            ItemCount = rows.Sum(r => r.ItemCount),
            TotalQuantity = rows.Sum(r => r.TotalQuantity),
            IncludeNotes = includeNotes,
            Rows = rows
        };
    }

    private static string BuildTitle(ReportPeriod period, DateOnly from, DateOnly to) => period switch
    {
        ReportPeriod.Daily => $"Günlük Liste — {from:dd.MM.yyyy}",
        ReportPeriod.Weekly => $"Haftalık Liste — {from:dd.MM.yyyy} / {to:dd.MM.yyyy}",
        ReportPeriod.Monthly => $"Aylık Liste — {from:MMMM yyyy}",
        ReportPeriod.Yearly => $"Yıllık Liste — {from:yyyy}",
        _ => throw new ArgumentOutOfRangeException(nameof(period), period, null)
    };

    private static string BuildOrderListTitle(OrderListFilter filter)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(filter.Search))
            parts.Add($"Arama: {filter.Search.Trim()}");

        if (filter.Scope != OrderListScope.None)
        {
            parts.Add(OrderScopes.Display(filter.Scope));
        }
        else
        {
            if (filter.Status.HasValue)
                parts.Add($"Durum: {DisplayNames.Status(filter.Status.Value)}");
            if (filter.From.HasValue || filter.To.HasValue)
            {
                var field = filter.DateField == ReportDateFieldKind.DeliveryDate ? "Teslim tarihi" : "Sipariş tarihi";
                parts.Add($"{field}: {filter.From?.ToString("dd.MM.yyyy") ?? "…"} – {filter.To?.ToString("dd.MM.yyyy") ?? "…"}");
            }
        }

        if (filter.DeliveryPlace.HasValue)
            parts.Add($"Teslim yeri: {DisplayNames.Place(filter.DeliveryPlace.Value)}");

        return parts.Count == 0
            ? "Sipariş Listesi — Tüm siparişler"
            : $"Sipariş Listesi — {string.Join("  |  ", parts)}";
    }
}
