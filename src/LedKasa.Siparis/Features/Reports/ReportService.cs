using LedKasa.Siparis.Common;
using LedKasa.Siparis.Data;
using LedKasa.Siparis.Features.Orders.Domain;
using Microsoft.EntityFrameworkCore;

namespace LedKasa.Siparis.Features.Reports;

public interface IReportService
{
    Task<ReportResult> GetAsync(ReportRequest request, CancellationToken cancellationToken = default);
}

public sealed class ReportService : IReportService
{
    private readonly ApplicationDbContext _db;

    public ReportService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ReportResult> GetAsync(ReportRequest request, CancellationToken cancellationToken = default)
    {
        var (from, to) = ReportPeriodCalculator.GetRange(request.Period, request.Anchor);
        var query = _db.Orders.AsNoTracking()
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
            GrandTotal = o.GrandTotal,
            ItemSummary = string.Join(" · ", o.Items.Select(i =>
                $"{i.Product?.Name ?? "Ürün"} {i.WidthCm:0.##}x{i.HeightCm:0.##} cm x{i.Quantity}"))
        }).ToList();

        var grandTotal = rows.Sum(r => r.GrandTotal);
        return new ReportResult
        {
            From = from,
            To = to,
            Title = BuildTitle(request.Period, from, to),
            OrderCount = rows.Count,
            ItemCount = rows.Sum(r => r.ItemCount),
            TotalQuantity = rows.Sum(r => r.TotalQuantity),
            GrandTotal = grandTotal,
            FormattedGrandTotal = TurkeyTime.FormatMoney(grandTotal),
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
}
