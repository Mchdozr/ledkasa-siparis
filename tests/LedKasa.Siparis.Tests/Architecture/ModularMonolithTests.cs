using FluentAssertions;
using LedKasa.Siparis.Common;
using LedKasa.Siparis.Features.Audit;
using LedKasa.Siparis.Features.Catalog;
using LedKasa.Siparis.Features.Dashboard;
using LedKasa.Siparis.Features.Orders;
using LedKasa.Siparis.Features.Orders.Domain;
using LedKasa.Siparis.Features.Reports;
using LedKasa.Siparis.Identity;
using LedKasa.Siparis.Security;
using NetArchTest.Rules;

namespace LedKasa.Siparis.Tests.Architecture;

public class ModularMonolithTests
{
    [Fact]
    public void Shared_types_live_in_Shared_assembly()
    {
        AssemblyOf(typeof(TurkeyTime)).Should().Be("LedKasa.Siparis.Shared");
        AssemblyOf(typeof(IsoWeekRange)).Should().Be("LedKasa.Siparis.Shared");
        AssemblyOf(typeof(DomainException)).Should().Be("LedKasa.Siparis.Shared");
        AssemblyOf(typeof(ICurrentUser)).Should().Be("LedKasa.Siparis.Shared");
        AssemblyOf(typeof(AuditLog)).Should().Be("LedKasa.Siparis.Shared");
    }

    [Fact]
    public void Product_lives_in_Catalog_assembly()
    {
        AssemblyOf(typeof(Product)).Should().Be("LedKasa.Siparis.Catalog");
        AssemblyOf(typeof(IProductService)).Should().Be("LedKasa.Siparis.Catalog");
        AssemblyOf(typeof(ProductService)).Should().Be("LedKasa.Siparis.Catalog");
    }

    [Fact]
    public void Orders_types_live_in_Orders_assembly()
    {
        AssemblyOf(typeof(Order)).Should().Be("LedKasa.Siparis.Orders");
        AssemblyOf(typeof(IOrderService)).Should().Be("LedKasa.Siparis.Orders");
        AssemblyOf(typeof(OrderService)).Should().Be("LedKasa.Siparis.Orders");
    }

    [Fact]
    public void Reporting_types_live_in_Reporting_assembly()
    {
        AssemblyOf(typeof(IReportService)).Should().Be("LedKasa.Siparis.Reporting");
        AssemblyOf(typeof(ReportService)).Should().Be("LedKasa.Siparis.Reporting");
        AssemblyOf(typeof(IDashboardService)).Should().Be("LedKasa.Siparis.Reporting");
        AssemblyOf(typeof(DashboardService)).Should().Be("LedKasa.Siparis.Reporting");
    }

    [Fact]
    public void Users_types_live_in_Users_assembly()
    {
        AssemblyOf(typeof(IUserAdminService)).Should().Be("LedKasa.Siparis.Users");
        AssemblyOf(typeof(UserAdminService)).Should().Be("LedKasa.Siparis.Users");
        AssemblyOf(typeof(ApplicationUser)).Should().Be("LedKasa.Siparis.Users");
    }

    [Fact]
    public void Catalog_does_not_reference_Orders_Reporting_or_Users()
    {
        Refs(typeof(Product)).Should().NotContain("LedKasa.Siparis.Orders");
        Refs(typeof(Product)).Should().NotContain("LedKasa.Siparis.Reporting");
        Refs(typeof(Product)).Should().NotContain("LedKasa.Siparis.Users");
    }

    [Fact]
    public void Orders_does_not_reference_Reporting_or_Users()
    {
        Refs(typeof(Order)).Should().NotContain("LedKasa.Siparis.Reporting");
        Refs(typeof(Order)).Should().NotContain("LedKasa.Siparis.Users");
    }

    [Fact]
    public void Users_does_not_reference_Orders_or_Reporting()
    {
        Refs(typeof(ApplicationUser)).Should().NotContain("LedKasa.Siparis.Orders");
        Refs(typeof(ApplicationUser)).Should().NotContain("LedKasa.Siparis.Reporting");
    }

    [Fact]
    public void Reporting_db_contract_cannot_save_changes()
    {
        typeof(IReportingDbContext).GetMembers()
            .Select(m => m.Name)
            .Should().NotContain(name => name.Contains("SaveChanges", StringComparison.Ordinal));
    }

    [Fact]
    public void Order_pages_do_not_use_reports_or_user_admin()
    {
        var assembly = typeof(Program).Assembly;

        Types.InAssembly(assembly)
            .That().ResideInNamespaceStartingWith("LedKasa.Siparis.Components.Pages.Orders")
            .ShouldNot().HaveDependencyOn("LedKasa.Siparis.Features.Reports")
            .GetResult().IsSuccessful.Should().BeTrue();

        Types.InAssembly(assembly)
            .That().ResideInNamespaceStartingWith("LedKasa.Siparis.Components.Pages.Orders")
            .ShouldNot().HaveDependencyOn(typeof(IUserAdminService).FullName)
            .GetResult().IsSuccessful.Should().BeTrue();
    }

    private static string? AssemblyOf(Type type) => type.Assembly.GetName().Name;

    private static IEnumerable<string> Refs(Type type) =>
        type.Assembly.GetReferencedAssemblies().Select(a => a.Name!);
}
