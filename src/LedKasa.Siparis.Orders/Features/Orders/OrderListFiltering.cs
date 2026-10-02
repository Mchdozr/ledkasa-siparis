using LedKasa.Siparis.Features.Orders.Domain;

namespace LedKasa.Siparis.Features.Orders;

public static class OrderListFiltering
{
    public static IQueryable<Order> ApplyFilter(this IQueryable<Order> query, OrderListFilter filter, DateOnly today)
    {
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(o => o.OrderNumber.Contains(term) || o.CustomerName.Contains(term));
        }

        if (filter.Scope != OrderListScope.None)
        {
            query = OrderScopes.Apply(query, filter.Scope, today);
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

        return query;
    }

    public static IOrderedQueryable<Order> ApplySort(this IQueryable<Order> query, OrderSort sort) =>
        sort switch
        {
            OrderSort.NewestFirst => query.OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.Id),
            OrderSort.OldestFirst => query.OrderBy(o => o.OrderDate).ThenBy(o => o.Id),
            OrderSort.NearestDelivery => query.OrderBy(o => o.DeliveryDate).ThenBy(o => o.Id),
            OrderSort.FarthestDelivery => query.OrderByDescending(o => o.DeliveryDate).ThenByDescending(o => o.Id),
            _ => throw new ArgumentOutOfRangeException(nameof(sort), sort, null)
        };
}
