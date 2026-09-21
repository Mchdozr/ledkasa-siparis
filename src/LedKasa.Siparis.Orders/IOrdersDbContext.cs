using LedKasa.Siparis.Features.Audit;
using LedKasa.Siparis.Features.Orders.Domain;
using Microsoft.EntityFrameworkCore;

namespace LedKasa.Siparis.Features.Orders;

public interface IOrdersDbContext
{
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<AuditLog> AuditLogs { get; }
    IQueryable<string> ActiveUserDisplayNames { get; }
    void SetOriginalRowVersion(Order order, DateTime rowVersion);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
