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
            Policies.OrdersChangeStatus,
            Policies.ReportsView, Policies.AdminUsers, Policies.AdminFeatures
        });
    }

    [Fact]
    public void Personel_ShouldCreateOrdersAndChangeStatus_ButNotAdmin()
    {
        var policies = Policies.ForRole(AppRoles.SiparisPersoneli);
        policies.Should().BeEquivalentTo(
        [
            Policies.OrdersView,
            Policies.OrdersCreate,
            Policies.OrdersEdit,
            Policies.OrdersChangeStatus
        ]);
        policies.Should().NotContain(Policies.ReportsView);
        policies.Should().NotContain(Policies.AdminUsers);
        policies.Should().NotContain(Policies.AdminFeatures);
    }

    [Fact]
    public void Muhasebe_ShouldOnlyViewAndReport()
    {
        var policies = Policies.ForRole(AppRoles.Muhasebe);
        policies.Should().BeEquivalentTo([Policies.OrdersView, Policies.ReportsView]);
    }
}
