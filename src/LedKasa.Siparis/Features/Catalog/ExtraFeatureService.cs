using LedKasa.Siparis.Data;
using LedKasa.Siparis.Features.Audit;
using LedKasa.Siparis.Features.Orders.Domain;
using LedKasa.Siparis.Security;
using Microsoft.EntityFrameworkCore;

namespace LedKasa.Siparis.Features.Catalog;

public sealed class ExtraFeatureDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
}

public interface IExtraFeatureService
{
    Task<IReadOnlyList<ExtraFeatureDto>> ListAsync(bool activeOnly, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(string name, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, string name, bool isActive, int sortOrder, CancellationToken cancellationToken = default);
}

public sealed class ExtraFeatureService : IExtraFeatureService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ExtraFeatureService(ApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ExtraFeatureDto>> ListAsync(bool activeOnly, CancellationToken cancellationToken = default)
    {
        var query = _db.ExtraFeatures.AsNoTracking().AsQueryable();
        if (activeOnly)
            query = query.Where(x => x.IsActive);

        return await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new ExtraFeatureDto
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
        var maxSort = await _db.ExtraFeatures.Select(x => (int?)x.SortOrder).MaxAsync(cancellationToken) ?? 0;
        var feature = ExtraFeature.Create(name, maxSort + 1);
        _db.ExtraFeatures.Add(feature);
        _db.AuditLogs.Add(new AuditLog
        {
            Action = "FeatureCreated",
            EntityType = "ExtraFeature",
            EntityId = name,
            UserId = _currentUser.UserId,
            Details = name,
            CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        return feature.Id;
    }

    public async Task UpdateAsync(int id, string name, bool isActive, int sortOrder, CancellationToken cancellationToken = default)
    {
        var feature = await _db.ExtraFeatures.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new DomainException("Özellik bulunamadı.");

        feature.Rename(name);
        feature.SetActive(isActive);
        feature.SetSortOrder(sortOrder);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
