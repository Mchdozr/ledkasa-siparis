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
            Policies.OrdersChangeStatus, Policies.OrdersDelete,
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
        policies.Should().NotContain(Policies.OrdersDelete);
        policies.Should().NotContain(Policies.AdminUsers);
        policies.Should().NotContain(Policies.AdminFeatures);
    }

    [Fact]
    public void Muhasebe_ShouldOnlyViewAndReport()
    {
        var policies = Policies.ForRole(AppRoles.Muhasebe);
        policies.Should().BeEquivalentTo([Policies.OrdersView, Policies.ReportsView]);
        Policies.ForRole(AppRoles.Muhasebe).Should().NotContain(Policies.OrdersDelete);
    }

    [Fact]
    public void TestUserName_ShouldBeAllowedToDelete()
    {
        var test = User("test", AppRoles.SiparisPersoneli);
        Policies.CanDeleteOrders(test).Should().BeTrue();
        Policies.CanDeleteOrders(User("TEST", AppRoles.Muhasebe)).Should().BeTrue();
        Policies.CanDeleteOrders(User("personel1", AppRoles.SiparisPersoneli)).Should().BeFalse();
        Policies.CanDeleteOrders(User("admin", AppRoles.Yonetici)).Should().BeTrue();
    }

    private static System.Security.Claims.ClaimsPrincipal User(string name, string role) =>
        new(new System.Security.Claims.ClaimsIdentity(
        [
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, name),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role)
        ], "test"));
}
