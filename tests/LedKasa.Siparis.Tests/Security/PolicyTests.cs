using FluentAssertions;
using LedKasa.Siparis.Identity;
using LedKasa.Siparis.Security;

namespace LedKasa.Siparis.Tests.Security;

public class PolicyTests
{
    [Fact]
    public void Yonetici_ShouldHaveAllPolicies()
    {
        Policies.ForRole(AppRoles.Yonetici).Should().Contain(new[]
        {
            Policies.OrdersView, Policies.OrdersCreate, Policies.OrdersEdit,
            Policies.OrdersChangeStatus, Policies.OrdersViewPrices,
            Policies.ReportsView, Policies.AdminUsers, Policies.AdminFeatures
        });
    }

    [Fact]
    public void Personel_ShouldNotSeeReportsOrAdmin()
    {
        var policies = Policies.ForRole(AppRoles.SiparisPersoneli);
        policies.Should().Contain(Policies.OrdersEdit);
        policies.Should().NotContain(Policies.OrdersCreate);
        policies.Should().NotContain(Policies.OrdersViewPrices);
        policies.Should().NotContain(Policies.ReportsView);
        policies.Should().NotContain(Policies.AdminUsers);
    }

    [Fact]
    public void Muhasebe_ShouldOnlyViewAndReport()
    {
        var policies = Policies.ForRole(AppRoles.Muhasebe);
        policies.Should().BeEquivalentTo([Policies.OrdersView, Policies.OrdersViewPrices, Policies.ReportsView]);
    }
}
