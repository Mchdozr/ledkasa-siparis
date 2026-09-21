using LedKasa.Siparis.Features.Catalog;
using LedKasa.Siparis.Features.Orders;
using LedKasa.Siparis.Features.Reports;
using Microsoft.EntityFrameworkCore;

namespace LedKasa.Siparis.Data;

internal sealed class ApplicationModuleDbContextFactory(IDbContextFactory<ApplicationDbContext> factory)
    : IOrdersDbContextFactory, ICatalogDbContextFactory, IReportingDbContextFactory
{
    IOrdersDbContext IOrdersDbContextFactory.CreateDbContext() => factory.CreateDbContext();

    ICatalogDbContext ICatalogDbContextFactory.CreateDbContext() => factory.CreateDbContext();

    IReportingDbContext IReportingDbContextFactory.CreateDbContext() => factory.CreateDbContext();
}
