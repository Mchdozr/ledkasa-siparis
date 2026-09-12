using LedKasa.Siparis.Common;
using LedKasa.Siparis.Data;
using LedKasa.Siparis.Features.Orders.Domain;
using Microsoft.EntityFrameworkCore;

namespace LedKasa.Siparis.Features.Orders;

public static class DemoOrderSeeder
{
    public const int DemoOrderCount = 27;

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
        if (await db.Orders.AnyAsync())
            return 0;

        var userId = await db.Users.AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.Id)
            .Select(u => u.Id)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(userId))
            return 0;

        var sequences = new Dictionary<DateOnly, int>();
        var orders = Specs.Select(spec =>
        {
            var orderDate = today.AddDays(spec.OrderOffset);
            var deliveryDate = orderDate.AddDays(spec.LeadDays);
            sequences[orderDate] = sequences.GetValueOrDefault(orderDate) + 1;
            var order = Order.Create(
                OrderNumberFormatter.Format(orderDate, sequences[orderDate]),
                spec.Customer,
                orderDate,
                deliveryDate,
                spec.Place,
                spec.Items.Select(i => OrderItem.Create(i.ProductId, i.Width, i.Height, i.Qty, i.Price, i.Extras, i.Note)),
                userId,
                spec.Notes,
                currency: spec.Currency);
            ApplyStatus(order, spec.Status, userId);
            return order;
        }).ToList();

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
        decimal Price,
        int[]? Extras = null,
        string? Note = null);

    private sealed record OrderSpec(
        string Customer,
        int OrderOffset,
        int LeadDays,
        DeliveryPlace Place,
        Currency Currency,
        OrderStatus Status,
        string? Notes,
        ItemSpec[] Items);

    private static readonly OrderSpec[] Specs =
    [
        new("İstanbul AVM A.Ş.", -2, 5, DeliveryPlace.Sirket, Currency.Try, OrderStatus.Yeni, "Giriş holü p1.25",
            [new(1, 50, 50, 12, 1850), new(2, 50, 100, 4, 3200, [3], "Askı aparatlı")]),
        new("Ankara Büyükşehir Belediyesi", -1, 8, DeliveryPlace.Fabrika, Currency.Try, OrderStatus.Yeni, "Meydan ekranı",
            [new(2, 96, 96, 8, 4100, [1, 4])]),
        new("İzmir Fuar Merkezi", 0, 4, DeliveryPlace.Sirket, Currency.Try, OrderStatus.Yeni, null,
            [new(3, 64, 48, 20, 1450, [2])]),
        new("Bursa Nilüfer AVM", 0, 6, DeliveryPlace.Fabrika, Currency.Usd, OrderStatus.Yeni, "Outdoor rental",
            [new(3, 50, 50, 16, 210)]),
        new("Antalya Expo", -3, 7, DeliveryPlace.Sirket, Currency.Eur, OrderStatus.Yeni, null,
            [new(4, 50, 200, 6, 890, [3, 5], "Poster kasa")]),
        new("Konya Selçuklu Belediyesi", -4, 10, DeliveryPlace.Fabrika, Currency.Try, OrderStatus.Onaylandi, "Belediye binası",
            [new(1, 80, 120, 4, 2750)]),
        new("Gaziantep Park AVM", -5, 6, DeliveryPlace.Sirket, Currency.Try, OrderStatus.Onaylandi, null,
            [new(2, 50, 50, 24, 1680, [1])]),
        new("Adana Merkez Cami Derneği", -6, 12, DeliveryPlace.Fabrika, Currency.Try, OrderStatus.Onaylandi, "Minare LED",
            [new(4, 32, 160, 8, 1950, [4, 5])]),
        new("Trabzon Stadyum İşletmesi", -7, 9, DeliveryPlace.Sirket, Currency.Usd, OrderStatus.Onaylandi, "Perimetre",
            [new(3, 96, 96, 10, 380)]),
        new("Eskişehir Tepebaşı", -3, 5, DeliveryPlace.Fabrika, Currency.Try, OrderStatus.Onaylandi, null,
            [new(1, 64, 48, 14, 1520, [6])]),
        new("Samsun Piazza", -8, 7, DeliveryPlace.Sirket, Currency.Try, OrderStatus.Uretimde, "Food court",
            [new(2, 50, 100, 10, 3100, [3])]),
        new("Mersin Marina", -9, 11, DeliveryPlace.Fabrika, Currency.Eur, OrderStatus.Uretimde, "Tuzlu ortam",
            [new(3, 50, 50, 18, 240, [4])]),
        new("Kayseri Forum", -10, 8, DeliveryPlace.Sirket, Currency.Try, OrderStatus.Uretimde, null,
            [new(1, 64, 64, 12, 1980), new(5, 32, 32, 6, 720)]),
        new("Diyarbakır Sur Belediyesi", -11, 14, DeliveryPlace.Fabrika, Currency.Try, OrderStatus.Uretimde, "Tarihi alan",
            [new(5, 80, 80, 6, 2600, [5])]),
        new("Bodrum Yalıkavak Marina", -6, 5, DeliveryPlace.Sirket, Currency.Usd, OrderStatus.Uretimde, "VIP lounge",
            [new(4, 50, 200, 3, 620, [3])]),
        new("Ankara Esenboğa Havalimanı", -12, 9, DeliveryPlace.Fabrika, Currency.Try, OrderStatus.Hazir, "Check-in",
            [new(1, 96, 54, 8, 4550, [1, 2])]),
        new("İstanbul Sabiha Gökçen", -13, 8, DeliveryPlace.Sirket, Currency.Usd, OrderStatus.Hazir, null,
            [new(3, 64, 48, 16, 195)]),
        new("Bursa Merinos AKKM", -14, 10, DeliveryPlace.Fabrika, Currency.Try, OrderStatus.Hazir, "Sahne arkası",
            [new(2, 50, 50, 30, 1720, [6])]),
        new("Çeşme Alaçatı Belediyesi", -9, 6, DeliveryPlace.Sirket, Currency.Eur, OrderStatus.Hazir, "Meydan",
            [new(5, 50, 100, 8, 410, [4])]),
        new("Kocaeli Gebze OSB", -18, 7, DeliveryPlace.Fabrika, Currency.Try, OrderStatus.TeslimEdildi, "Fabrika giriş",
            [new(1, 80, 120, 6, 2480)]),
        new("İzmir Alsancak Liman", -20, 8, DeliveryPlace.Sirket, Currency.Usd, OrderStatus.TeslimEdildi, null,
            [new(3, 96, 96, 4, 360, [1])]),
        new("Antalya Kepez Belediyesi", -22, 12, DeliveryPlace.Fabrika, Currency.Try, OrderStatus.TeslimEdildi, "Park alanı",
            [new(4, 50, 200, 5, 3350, [3, 4])]),
        new("Sakarya Serdivan AVM", -16, 6, DeliveryPlace.Sirket, Currency.Try, OrderStatus.TeslimEdildi, null,
            [new(2, 64, 48, 18, 1410)]),
        new("Denizli Teras Park", -24, 9, DeliveryPlace.Fabrika, Currency.Try, OrderStatus.TeslimEdildi, "Kat holü",
            [new(5, 50, 50, 22, 1690, [2])]),
        new("Van İpekyolu Belediyesi", -26, 15, DeliveryPlace.Sirket, Currency.Try, OrderStatus.TeslimEdildi, "Belediye meydanı",
            [new(1, 80, 80, 8, 2550, [5])]),
        new("Rize Çaykur Tesisleri", -15, 5, DeliveryPlace.Fabrika, Currency.Try, OrderStatus.Iptal, "Ölçü revizyonu bekleniyor",
            [new(3, 50, 50, 10, 1750)]),
        new("Hatay Defne Belediyesi", -19, 8, DeliveryPlace.Sirket, Currency.Eur, OrderStatus.Iptal, "Bütçe iptali",
            [new(4, 64, 64, 6, 280)])
    ];
}
