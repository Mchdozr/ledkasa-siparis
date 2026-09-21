using LedKasa.Siparis.Data;
using LedKasa.Siparis.Features.Catalog;
using LedKasa.Siparis.Features.Orders;
using LedKasa.Siparis.Features.Reports;
using Microsoft.EntityFrameworkCore;

namespace LedKasa.Siparis.Tests.Infrastructure;

internal sealed class TestDbStore : IAsyncDisposable
{
    public TestDbStore(ApplicationDbContext db, TestDbContextFactory factory)
    {
        Db = db;
        Factory = factory;
    }

    public ApplicationDbContext Db { get; }
    public TestDbContextFactory Factory { get; }
    public IOrdersDbContextFactory Orders => Factory;
    public ICatalogDbContextFactory Catalog => Factory;
    public IReportingDbContextFactory Reporting => Factory;

    public ValueTask DisposeAsync() => Db.DisposeAsync();
}

internal sealed class TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
    : IDbContextFactory<ApplicationDbContext>, IOrdersDbContextFactory, ICatalogDbContextFactory, IReportingDbContextFactory
{
    public ApplicationDbContext CreateDbContext() => new(options);

    IOrdersDbContext IOrdersDbContextFactory.CreateDbContext() => CreateDbContext();

    ICatalogDbContext ICatalogDbContextFactory.CreateDbContext() => CreateDbContext();

    IReportingDbContext IReportingDbContextFactory.CreateDbContext() => CreateDbContext();
}

internal static class TestDb
{
    public static ApplicationDbContext Create() => CreateStore().Db;

    public static TestDbStore CreateStore()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"lk-{Guid.NewGuid():N}")
            .EnableSensitiveDataLogging()
            .Options;

        var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();
        return new TestDbStore(db, new TestDbContextFactory(options));
    }
}
