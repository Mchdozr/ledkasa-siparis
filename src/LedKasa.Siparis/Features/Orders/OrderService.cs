using FluentValidation;
using LedKasa.Siparis.Data;
using LedKasa.Siparis.Features.Audit;
using LedKasa.Siparis.Features.Orders.Domain;
using LedKasa.Siparis.Security;
using Microsoft.EntityFrameworkCore;

namespace LedKasa.Siparis.Features.Orders;

public interface IOrderService
{
    Task<PagedResult<OrderListItemDto>> ListAsync(OrderListFilter filter, CancellationToken cancellationToken = default);
    Task<OrderDetailDto?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(OrderDraft draft, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, OrderDraft draft, DateTime rowVersion, CancellationToken cancellationToken = default);
    Task ChangeStatusAsync(int id, OrderStatus next, DateTime rowVersion, CancellationToken cancellationToken = default);
}

public sealed class OrderService : IOrderService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<OrderDraft> _validator;

    public OrderService(ApplicationDbContext db, ICurrentUser currentUser, IValidator<OrderDraft> validator)
    {
        _db = db;
        _currentUser = currentUser;
        _validator = validator;
    }

    public async Task<PagedResult<OrderListItemDto>> ListAsync(OrderListFilter filter, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 5, 100);

        var query = _db.Orders.AsNoTracking().Include(o => o.Items).AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(o => o.OrderNumber.Contains(term) || o.CustomerName.Contains(term));
        }

        if (filter.Status.HasValue)
            query = query.Where(o => o.Status == filter.Status.Value);

        if (filter.DeliveryPlace.HasValue)
            query = query.Where(o => o.DeliveryPlace == filter.DeliveryPlace.Value);

        if (filter.DateField == ReportDateFieldKind.DeliveryDate)
        {
            if (filter.From.HasValue)
                query = query.Where(o => o.DeliveryDate >= filter.From.Value);
            if (filter.To.HasValue)
                query = query.Where(o => o.DeliveryDate <= filter.To.Value);
        }
        else
        {
            if (filter.From.HasValue)
                query = query.Where(o => o.OrderDate >= filter.From.Value);
            if (filter.To.HasValue)
                query = query.Where(o => o.OrderDate <= filter.To.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(o => o.OrderDate)
            .ThenByDescending(o => o.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderListItemDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerName = o.CustomerName,
                OrderDate = o.OrderDate,
                DeliveryDate = o.DeliveryDate,
                DeliveryPlace = o.DeliveryPlace,
                Currency = o.Currency,
                Status = o.Status,
                ItemCount = o.Items.Count,
                TotalQuantity = o.Items.Sum(i => i.Quantity),
                GrandTotal = o.GrandTotal
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<OrderListItemDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<OrderDetailDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
                .ThenInclude(i => i.ExtraFeatures)
                    .ThenInclude(l => l.ExtraFeature)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        return order is null ? null : MapDetail(order);
    }

    public async Task<int> CreateAsync(OrderDraft draft, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(draft, cancellationToken);

        var items = draft.Items.Select(ToItem).ToList();
        var number = await NextNumberAsync(draft.OrderDate, cancellationToken);
        var order = Order.Create(
            number,
            draft.CustomerName,
            draft.OrderDate,
            draft.DeliveryDate,
            draft.DeliveryPlace,
            items,
            _currentUser.UserId,
            draft.Notes,
            currency: draft.Currency);

        _db.Orders.Add(order);
        AddAudit("OrderCreated", "Order", number, $"Müşteri: {order.CustomerName}");
        await _db.SaveChangesAsync(cancellationToken);
        return order.Id;
    }

    public async Task UpdateAsync(int id, OrderDraft draft, DateTime rowVersion, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(draft, cancellationToken);

        var order = await LoadTrackedAsync(id, cancellationToken);
        _db.Entry(order).Property(x => x.RowVersion).OriginalValue = rowVersion;

        order.UpdateHeader(
            draft.CustomerName,
            draft.OrderDate,
            draft.DeliveryDate,
            draft.DeliveryPlace,
            draft.Currency,
            draft.Notes,
            _currentUser.UserId);

        _db.OrderItems.RemoveRange(order.Items);
        order.ReplaceItems(draft.Items.Select(ToItem), _currentUser.UserId);

        AddAudit("OrderUpdated", "Order", order.Id.ToString(), order.OrderNumber);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangeStatusAsync(int id, OrderStatus next, DateTime rowVersion, CancellationToken cancellationToken = default)
    {
        var order = await LoadTrackedAsync(id, cancellationToken);
        _db.Entry(order).Property(x => x.RowVersion).OriginalValue = rowVersion;
        var previous = order.Status;
        order.TransitionTo(next, _currentUser.UserId);
        AddAudit("OrderStatusChanged", "Order", order.Id.ToString(), $"{DisplayNames.Status(previous)} → {DisplayNames.Status(next)}");
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Order> LoadTrackedAsync(int id, CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.ExtraFeatures)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        return order ?? throw new DomainException("Sipariş bulunamadı.");
    }

    private async Task<string> NextNumberAsync(DateOnly orderDate, CancellationToken cancellationToken)
    {
        var prefix = $"LK-{orderDate:yyyyMMdd}-";
        var last = await _db.Orders
            .Where(o => o.OrderNumber.StartsWith(prefix))
            .Select(o => o.OrderNumber)
            .OrderByDescending(n => n)
            .FirstOrDefaultAsync(cancellationToken);

        var next = 1;
        if (last is not null && int.TryParse(last[^4..], out var parsed))
            next = parsed + 1;

        return OrderNumberFormatter.Format(orderDate, next);
    }

    private static OrderItem ToItem(OrderItemInput input) =>
        OrderItem.Create(input.WidthCm, input.HeightCm, input.Quantity, input.UnitPrice, input.ExtraFeatureIds, input.Note);

    private void AddAudit(string action, string entityType, string entityId, string? details)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            UserId = _currentUser.UserId,
            Details = details,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    private static OrderDetailDto MapDetail(Order order) => new()
    {
        Id = order.Id,
        OrderNumber = order.OrderNumber,
        CustomerName = order.CustomerName,
        OrderDate = order.OrderDate,
        DeliveryDate = order.DeliveryDate,
        DeliveryPlace = order.DeliveryPlace,
        Currency = order.Currency,
        Status = order.Status,
        Notes = order.Notes,
        CreatedByUserId = order.CreatedByUserId,
        CreatedAtUtc = order.CreatedAtUtc,
        UpdatedAtUtc = order.UpdatedAtUtc,
        RowVersion = order.RowVersion,
        GrandTotal = order.GrandTotal,
        CanEdit = order.CanEditDetails,
        AllowedNextStatuses = OrderStatusTransitions.AllowedFrom(order.Status),
        Items = order.Items.Select(i => new OrderItemDto
        {
            Id = i.Id,
            WidthCm = i.WidthCm,
            HeightCm = i.HeightCm,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            LineTotal = i.LineTotal,
            Note = i.Note,
            ExtraFeatureIds = i.ExtraFeatures.Select(f => f.ExtraFeatureId).ToList(),
            ExtraFeatureNames = i.ExtraFeatures
                .Select(f => f.ExtraFeature?.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Cast<string>()
                .ToList()
        }).ToList()
    };
}
