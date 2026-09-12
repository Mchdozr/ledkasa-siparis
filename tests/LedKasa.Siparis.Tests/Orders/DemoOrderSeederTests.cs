using FluentAssertions;
using LedKasa.Siparis.Features.Orders;
using LedKasa.Siparis.Features.Orders.Domain;
using LedKasa.Siparis.Identity;
using LedKasa.Siparis.Tests.Infrastructure;

namespace LedKasa.Siparis.Tests.Orders;

public class DemoOrderSeederTests
{
    [Fact]
    public async Task Seed_ShouldInsertTwentySevenOrders_WhenEmpty()
    {
        await using var db = TestDb.Create();
        db.Users.Add(new ApplicationUser
        {
            Id = "admin-1",
            UserName = "admin@ledkasa.com.tr",
            Email = "admin@ledkasa.com.tr",
            DisplayName = "LEDKASA Yönetici",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var added = await DemoOrderSeeder.SeedAsync(db, new DateOnly(2026, 9, 12));

        added.Should().Be(27);
        db.Orders.Should().HaveCount(27);
        db.Orders.Select(o => o.OrderNumber).Distinct().Should().HaveCount(27);
        db.Orders.Count(o => o.Status == OrderStatus.Yeni).Should().Be(5);
        db.Orders.Count(o => o.Status == OrderStatus.Onaylandi).Should().Be(5);
        db.Orders.Count(o => o.Status == OrderStatus.Uretimde).Should().Be(5);
        db.Orders.Count(o => o.Status == OrderStatus.Hazir).Should().Be(4);
        db.Orders.Count(o => o.Status == OrderStatus.TeslimEdildi).Should().Be(6);
        db.Orders.Count(o => o.Status == OrderStatus.Iptal).Should().Be(2);
        db.OrderItems.Should().HaveCountGreaterThan(27);
        db.OrderItems.Select(i => i.ProductId).Distinct().Should().BeEquivalentTo([1, 2, 3, 4, 5]);
    }

    [Fact]
    public async Task Seed_ShouldKeepExisting_AndAddMissingDemos()
    {
        await using var db = TestDb.Create();
        db.Users.Add(new ApplicationUser
        {
            Id = "admin-1",
            UserName = "admin@ledkasa.com.tr",
            Email = "admin@ledkasa.com.tr",
            IsActive = true
        });
        db.Orders.Add(Order.Create(
            "LK-20260912-0001",
            "Mevcut",
            new DateOnly(2026, 9, 12),
            new DateOnly(2026, 9, 14),
            DeliveryPlace.Sirket,
            "Mevcut teslimat adresi",
            [OrderItem.Create(1, 50, 50, 1, 100)],
            "admin-1"));
        await db.SaveChangesAsync();

        var added = await DemoOrderSeeder.SeedAsync(db, new DateOnly(2026, 9, 12));

        added.Should().Be(27);
        db.Orders.Should().HaveCount(28);
        db.Orders.Count(o => o.CustomerName == "Mevcut").Should().Be(1);
        db.Orders.Count(o => o.OrderNumber.StartsWith("LK-DEMO-")).Should().Be(27);
    }

    [Fact]
    public async Task Seed_ShouldNotDuplicate_DemoOrders()
    {
        await using var db = TestDb.Create();
        db.Users.Add(new ApplicationUser
        {
            Id = "admin-1",
            UserName = "admin@ledkasa.com.tr",
            Email = "admin@ledkasa.com.tr",
            IsActive = true
        });
        await db.SaveChangesAsync();

        (await DemoOrderSeeder.SeedAsync(db, new DateOnly(2026, 9, 12))).Should().Be(27);
        (await DemoOrderSeeder.SeedAsync(db, new DateOnly(2026, 9, 12))).Should().Be(0);
        db.Orders.Should().HaveCount(27);
    }

    [Fact]
    public async Task Seed_ShouldSkip_WhenNoUser()
    {
        await using var db = TestDb.Create();
        var added = await DemoOrderSeeder.SeedAsync(db, new DateOnly(2026, 9, 12));
        added.Should().Be(0);
    }
}
