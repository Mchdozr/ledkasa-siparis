using FluentValidation;
using LedKasa.Siparis.Common;
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

        if (filter.Scope != OrderListScope.None)
        {
            query = OrderScopes.Apply(query, filter.Scope, TurkeyTime.Today);
        }
        else
        {
            if (filter.Status.HasValue)
                query = query.Where(o => o.Status == filter.Status.Value);

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
        }

        if (filter.DeliveryPlace.HasValue)
            query = query.Where(o => o.DeliveryPlace == filter.DeliveryPlace.Value);

        var total = await query.CountAsync(cancellationToken);
        var canViewPrices = _currentUser.CanViewPrices;
        var items = await ApplySort(query, filter.Sort)
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
                Currency = canViewPrices ? o.Currency : Currency.Try,
                Status = o.Status,
                ItemCount = o.Items.Count,
                TotalQuantity = o.Items.Sum(i => i.Quantity),
                GrandTotal = canViewPrices ? o.GrandTotal : 0
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
                .ThenInclude(i => i.Product)
            .Include(o => o.Items)
                .ThenInclude(i => i.ExtraFeatures)
                    .ThenInclude(l => l.ExtraFeature)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        return order is null ? null : MapDetail(order);
    }

    public async Task<int> CreateAsync(OrderDraft draft, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.CanCreateOrders)
            throw new DomainException("Sipariş oluşturma yetkiniz yok.");

        await _validator.ValidateAndThrowAsync(draft, cancellationToken);

        var items = draft.Items.Select(input => ToItem(ApplyPricePolicy(input))).ToList();
        var number = await NextNumberAsync(draft.OrderDate, cancellationToken);
        var order = Order.Create(
            number,
            draft.CustomerName,
            draft.OrderDate,
            draft.DeliveryDate,
            draft.DeliveryPlace,
            draft.DeliveryAddress,
            items,
            _currentUser.UserId,
            draft.Notes,
            currency: ApplyCurrencyPolicy(draft.Currency));

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
        var existingPrices = order.Items.ToDictionary(i => i.Id, i => i.UnitPrice);

        order.UpdateHeader(
            draft.CustomerName,
            draft.OrderDate,
            draft.DeliveryDate,
            draft.DeliveryPlace,
            draft.DeliveryAddress,
            ApplyCurrencyPolicy(draft.Currency, order.Currency),
            draft.Notes,
            _currentUser.UserId);

        _db.OrderItems.RemoveRange(order.Items);
        order.ReplaceItems(draft.Items.Select(input => ToItem(ApplyPricePolicy(input, existingPrices))), _currentUser.UserId);

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

    private static IOrderedQueryable<Order> ApplySort(IQueryable<Order> query, OrderSort sort) =>
        sort switch
        {
            OrderSort.NewestFirst => query.OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.Id),
            OrderSort.OldestFirst => query.OrderBy(o => o.OrderDate).ThenBy(o => o.Id),
            OrderSort.NearestDelivery => query.OrderBy(o => o.DeliveryDate).ThenBy(o => o.Id),
            OrderSort.FarthestDelivery => query.OrderByDescending(o => o.DeliveryDate).ThenByDescending(o => o.Id),
            _ => throw new ArgumentOutOfRangeException(nameof(sort), sort, null)
        };

    private static OrderItem ToItem(OrderItemInput input) =>
        OrderItem.Create(input.ProductId, input.WidthCm, input.HeightCm, input.Quantity, input.UnitPrice, input.ExtraFeatureIds, input.Note);

    private Currency ApplyCurrencyPolicy(Currency requested, Currency? existing = null) =>
        _currentUser.CanViewPrices ? requested : existing ?? Currency.Usd;

    private OrderItemInput ApplyPricePolicy(OrderItemInput input, IReadOnlyDictionary<int, decimal>? existingPrices = null)
    {
        if (_currentUser.CanViewPrices)
            return input;

        input.UnitPrice = input.Id != 0 && existingPrices is not null && existingPrices.TryGetValue(input.Id, out var price)
            ? price
            : 0;
        return input;
    }

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

    private OrderDetailDto MapDetail(Order order)
    {
        var canViewPrices = _currentUser.CanViewPrices;
        return new()
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerName = order.CustomerName,
            OrderDate = order.OrderDate,
            DeliveryDate = order.DeliveryDate,
            DeliveryPlace = order.DeliveryPlace,
            DeliveryAddress = order.DeliveryAddress,
            Currency = canViewPrices ? order.Currency : Currency.Try,
            Status = order.Status,
            Notes = order.Notes,
            CreatedByUserId = order.CreatedByUserId,
            CreatedAtUtc = order.CreatedAtUtc,
            UpdatedAtUtc = order.UpdatedAtUtc,
            RowVersion = order.RowVersion,
            GrandTotal = canViewPrices ? order.GrandTotal : 0,
            CanEdit = order.CanEditDetails,
            AllowedNextStatuses = OrderStatusTransitions.AllowedFrom(order.Status),
            Items = order.Items.Select(i => new OrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? string.Empty,
                WidthCm = i.WidthCm,
                HeightCm = i.HeightCm,
                Quantity = i.Quantity,
                UnitPrice = canViewPrices ? i.UnitPrice : 0,
                LineTotal = canViewPrices ? i.LineTotal : 0,
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
}
