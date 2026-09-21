using LedKasa.Siparis.Features.Orders.Domain;

namespace LedKasa.Siparis.Features.Reports;

public interface IReportingDbContext : IAsyncDisposable
{
    IQueryable<Order> Orders { get; }
}

public interface IReportingDbContextFactory
{
    IReportingDbContext CreateDbContext();
}
