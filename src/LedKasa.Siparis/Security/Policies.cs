using LedKasa.Siparis.Identity;
using Microsoft.AspNetCore.Authorization;

namespace LedKasa.Siparis.Security;

public static class Policies
{
    public const string OrdersView = "Orders.View";
    public const string OrdersCreate = "Orders.Create";
    public const string OrdersEdit = "Orders.Edit";
    public const string OrdersChangeStatus = "Orders.ChangeStatus";
    public const string OrdersViewPrices = "Orders.ViewPrices";
    public const string ReportsView = "Reports.View";
    public const string AdminUsers = "Admin.Users";
    public const string AdminFeatures = "Admin.Features";

    public static IEnumerable<string> ForRole(string role) => role switch
    {
        AppRoles.Yonetici =>
        [
            OrdersView, OrdersCreate, OrdersEdit, OrdersChangeStatus, OrdersViewPrices,
            ReportsView, AdminUsers, AdminFeatures
        ],
        AppRoles.SiparisPersoneli =>
        [
            OrdersView, OrdersCreate, OrdersEdit, OrdersChangeStatus
        ],
        AppRoles.Muhasebe =>
        [
            OrdersView, OrdersViewPrices, ReportsView
        ],
        _ => []
    };

    public static void AddAppPolicies(this AuthorizationOptions options)
    {
        foreach (var policy in All)
        {
            options.AddPolicy(policy, builder =>
            {
                builder.RequireAuthenticatedUser();
                builder.RequireAssertion(ctx =>
                    ctx.User.IsInRole(AppRoles.Yonetici) ||
                    RoleGrantsPolicy(ctx.User, policy));
            });
        }
    }

    private static readonly string[] All =
    [
        OrdersView, OrdersCreate, OrdersEdit, OrdersChangeStatus, OrdersViewPrices,
        ReportsView, AdminUsers, AdminFeatures
    ];

    private static bool RoleGrantsPolicy(System.Security.Claims.ClaimsPrincipal user, string policy)
    {
        if (user.IsInRole(AppRoles.SiparisPersoneli))
            return ForRole(AppRoles.SiparisPersoneli).Contains(policy);
        if (user.IsInRole(AppRoles.Muhasebe))
            return ForRole(AppRoles.Muhasebe).Contains(policy);
        return false;
    }
}
