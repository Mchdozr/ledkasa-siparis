using LedKasa.Siparis.Features.Audit;
using LedKasa.Siparis.Features.Catalog;
using LedKasa.Siparis.Features.Orders;
using LedKasa.Siparis.Features.Orders.Domain;
using LedKasa.Siparis.Features.Reports;
using LedKasa.Siparis.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LedKasa.Siparis.Data;

public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IOrdersDbContext, ICatalogDbContext, IReportingDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    IQueryable<string> IOrdersDbContext.ActiveUserDisplayNames =>
        Users.AsNoTracking().Where(u => u.IsActive).Select(u => u.DisplayName);

    IQueryable<Order> IReportingDbContext.Orders => Orders;

    public void SetOriginalRowVersion(Order order, DateTime rowVersion) =>
        Entry(order).Property(x => x.RowVersion).OriginalValue = rowVersion;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
