using LedKasa.Siparis.Features.Orders.Domain;

namespace LedKasa.Siparis.Features.Reports;

public interface IReportingDbContext
{
    IQueryable<Order> Orders { get; }
}
