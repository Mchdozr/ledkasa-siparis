using LedKasa.Siparis.Common;
using LedKasa.Siparis.Data;
using LedKasa.Siparis.Features.Orders.Domain;
using Microsoft.EntityFrameworkCore;

namespace LedKasa.Siparis.Features.Orders;

public static class DemoOrderSeeder
{
    public const int DemoOrderCount = 27;
    public const string DemoNumberPrefix = "LK-DEMO-";

    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var added = await SeedAsync(db, TurkeyTime.Today);
        if (added > 0)
            logger.LogInformation("Demo siparişler eklendi: {Count}", added);
    }

    public static async Task<int> SeedAsync(ApplicationDbContext db, DateOnly today)
    {
        var userId = await db.Users.AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.Id)
            .Select(u => u.Id)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(userId))
            return 0;

        var existing = await db.Orders.AsNoTracking()
            .Where(o => o.OrderNumber.StartsWith(DemoNumberPrefix))
            .Select(o => o.OrderNumber)
            .ToListAsync();

        var orders = Specs.Select((spec, index) =>
        {
            var number = $"{DemoNumberPrefix}{(index + 1):0000}";
            if (existing.Contains(number))
                return null;

            var orderDate = today.AddDays(spec.OrderOffset);
            var deliveryDate = orderDate.AddDays(spec.LeadDays);
            var order = Order.Create(
                number,
                spec.Customer,
                orderDate,
                deliveryDate,
                spec.Place,
                spec.Items.Select(i => OrderItem.Create(i.ProductId, i.Width, i.Height, i.Qty, i.Extras, i.Note)),
                userId,
                spec.Notes);
            ApplyStatus(order, spec.Status, userId);
            return order;
        }).Where(o => o is not null).Cast<Order>().ToList();

        if (orders.Count == 0)
            return 0;

        db.Orders.AddRange(orders);
        await db.SaveChangesAsync();
        return orders.Count;
    }

    private static void ApplyStatus(Order order, OrderStatus target, string userId)
    {
        var steps = target switch
        {
            OrderStatus.Yeni => Array.Empty<OrderStatus>(),
            OrderStatus.Onaylandi => new[] { OrderStatus.Onaylandi },
            OrderStatus.Uretimde => new[] { OrderStatus.Onaylandi, OrderStatus.Uretimde },
            OrderStatus.Hazir => new[] { OrderStatus.Onaylandi, OrderStatus.Uretimde, OrderStatus.Hazir },
            OrderStatus.TeslimEdildi => new[]
            {
                OrderStatus.Onaylandi, OrderStatus.Uretimde, OrderStatus.Hazir, OrderStatus.TeslimEdildi
            },
            OrderStatus.Iptal => new[] { OrderStatus.Iptal },
            _ => throw Unexpected(target)
        };

        foreach (var next in steps)
            order.TransitionTo(next, userId);
    }

    private static ArgumentOutOfRangeException Unexpected(OrderStatus status)
    {
        OrderStatus unused = status;
        throw new ArgumentOutOfRangeException(nameof(status), unused, null);
    }

    private sealed record ItemSpec(
        int ProductId,
        decimal Width,
        decimal Height,
        int Qty,
        int[]? Extras = null,
        string? Note = null);

    private sealed record OrderSpec(
        string Customer,
        int OrderOffset,
        int LeadDays,
        DeliveryPlace Place,
        OrderStatus Status,
        string? Notes,
        ItemSpec[] Items);

    private static readonly OrderSpec[] Specs =
    [
        new("İstanbul AVM A.Ş.", -2, 5, DeliveryPlace.Sirket, OrderStatus.Yeni, "Giriş holü p1.25",
            [new(1, 50, 50, 12), new(2, 50, 100, 4, [3], "Askı aparatlı")]),
        new("Ankara Büyükşehir Belediyesi", -1, 8, DeliveryPlace.Fabrika, OrderStatus.Yeni, "Meydan ekranı",
            [new(2, 96, 96, 8, [1, 4])]),
        new("İzmir Fuar Merkezi", 0, 4, DeliveryPlace.Sirket, OrderStatus.Yeni, null,
            [new(3, 64, 48, 20, [2])]),
        new("Bursa Nilüfer AVM", 0, 6, DeliveryPlace.Fabrika, OrderStatus.Yeni, "Outdoor rental",
            [new(3, 50, 50, 16)]),
        new("Antalya Expo", -3, 7, DeliveryPlace.Sirket, OrderStatus.Yeni, null,
            [new(4, 50, 200, 6, [3, 5], "Poster kasa")]),
        new("Konya Selçuklu Belediyesi", -4, 10, DeliveryPlace.Fabrika, OrderStatus.Onaylandi, "Belediye binası",
            [new(1, 80, 120, 4)]),
        new("Gaziantep Park AVM", -5, 6, DeliveryPlace.Sirket, OrderStatus.Onaylandi, null,
            [new(2, 50, 50, 24, [1])]),
        new("Adana Merkez Cami Derneği", -6, 12, DeliveryPlace.Fabrika, OrderStatus.Onaylandi, "Minare LED",
            [new(4, 32, 160, 8, [4, 5])]),
        new("Trabzon Stadyum İşletmesi", -7, 9, DeliveryPlace.Sirket, OrderStatus.Onaylandi, "Perimetre",
            [new(3, 96, 96, 10)]),
        new("Eskişehir Tepebaşı", -3, 5, DeliveryPlace.Fabrika, OrderStatus.Onaylandi, null,
            [new(1, 64, 48, 14, [6])]),
        new("Samsun Piazza", -8, 7, DeliveryPlace.Sirket, OrderStatus.Uretimde, "Food court",
            [new(2, 50, 100, 10, [3])]),
        new("Mersin Marina", -9, 11, DeliveryPlace.Fabrika, OrderStatus.Uretimde, "Tuzlu ortam",
            [new(3, 50, 50, 18, [4])]),
        new("Kayseri Forum", -10, 8, DeliveryPlace.Sirket, OrderStatus.Uretimde, null,
            [new(1, 64, 64, 12), new(5, 32, 32, 6)]),
        new("Diyarbakır Sur Belediyesi", -11, 14, DeliveryPlace.Fabrika, OrderStatus.Uretimde, "Tarihi alan",
            [new(5, 80, 80, 6, [5])]),
        new("Bodrum Yalıkavak Marina", -6, 5, DeliveryPlace.Sirket, OrderStatus.Uretimde, "VIP lounge",
            [new(4, 50, 200, 3, [3])]),
        new("Ankara Esenboğa Havalimanı", -12, 9, DeliveryPlace.Fabrika, OrderStatus.Hazir, "Check-in",
            [new(1, 96, 54, 8, [1, 2])]),
        new("İstanbul Sabiha Gökçen", -13, 8, DeliveryPlace.Sirket, OrderStatus.Hazir, null,
            [new(3, 64, 48, 16)]),
        new("Bursa Merinos AKKM", -14, 10, DeliveryPlace.Fabrika, OrderStatus.Hazir, "Sahne arkası",
            [new(2, 50, 50, 30, [6])]),
        new("Çeşme Alaçatı Belediyesi", -9, 6, DeliveryPlace.Sirket, OrderStatus.Hazir, "Meydan",
            [new(5, 50, 100, 8, [4])]),
        new("Kocaeli Gebze OSB", -18, 7, DeliveryPlace.Fabrika, OrderStatus.TeslimEdildi, "Fabrika giriş",
            [new(1, 80, 120, 6)]),
        new("İzmir Alsancak Liman", -20, 8, DeliveryPlace.Sirket, OrderStatus.TeslimEdildi, null,
            [new(3, 96, 96, 4, [1])]),
        new("Antalya Kepez Belediyesi", -22, 12, DeliveryPlace.Fabrika, OrderStatus.TeslimEdildi, "Park alanı",
            [new(4, 50, 200, 5, [3, 4])]),
        new("Sakarya Serdivan AVM", -16, 6, DeliveryPlace.Sirket, OrderStatus.TeslimEdildi, null,
            [new(2, 64, 48, 18)]),
        new("Denizli Teras Park", -24, 9, DeliveryPlace.Fabrika, OrderStatus.TeslimEdildi, "Kat holü",
            [new(5, 50, 50, 22, [2])]),
        new("Van İpekyolu Belediyesi", -26, 15, DeliveryPlace.Sirket, OrderStatus.TeslimEdildi, "Belediye meydanı",
            [new(1, 80, 80, 8, [5])]),
        new("Rize Çaykur Tesisleri", -15, 5, DeliveryPlace.Fabrika, OrderStatus.Iptal, "Ölçü revizyonu bekleniyor",
            [new(3, 50, 50, 10)]),
        new("Hatay Defne Belediyesi", -19, 8, DeliveryPlace.Sirket, OrderStatus.Iptal, "Bütçe iptali",
            [new(4, 64, 64, 6)])
    ];
}
