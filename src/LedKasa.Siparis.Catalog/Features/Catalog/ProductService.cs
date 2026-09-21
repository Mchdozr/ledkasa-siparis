using LedKasa.Siparis.Features.Audit;
using LedKasa.Siparis.Features.Orders.Domain;
using LedKasa.Siparis.Security;
using Microsoft.EntityFrameworkCore;

namespace LedKasa.Siparis.Features.Catalog;

public sealed class ProductDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
}

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> ListAsync(bool activeOnly, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(string name, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, string name, bool isActive, int sortOrder, CancellationToken cancellationToken = default);
}

internal sealed class ProductService : IProductService
{
    private readonly ICatalogDbContextFactory _dbFactory;
    private readonly ICurrentUser _currentUser;

    public ProductService(ICatalogDbContextFactory dbFactory, ICurrentUser currentUser)
    {
        _dbFactory = dbFactory;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ProductDto>> ListAsync(bool activeOnly, CancellationToken cancellationToken = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var query = db.Products.AsNoTracking().AsQueryable();
        if (activeOnly)
            query = query.Where(x => x.IsActive);

        return await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new ProductDto
            {
                Id = x.Id,
                Name = x.Name,
                IsActive = x.IsActive,
                SortOrder = x.SortOrder
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var maxSort = await db.Products.Select(x => (int?)x.SortOrder).MaxAsync(cancellationToken) ?? 0;
        var product = Product.Create(name, maxSort + 1);
        db.Products.Add(product);
        db.AuditLogs.Add(new AuditLog
        {
            Action = "ProductCreated",
            EntityType = "Product",
            EntityId = name,
            UserId = _currentUser.UserId,
            Details = name,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        return product.Id;
    }

    public async Task UpdateAsync(int id, string name, bool isActive, int sortOrder, CancellationToken cancellationToken = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var product = await db.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new DomainException("Ürün bulunamadı.");

        product.Rename(name);
        product.SetActive(isActive);
        product.SetSortOrder(sortOrder);
        await db.SaveChangesAsync(cancellationToken);
    }
}
