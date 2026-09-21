using System.Security.Claims;
using FluentAssertions;
using LedKasa.Siparis.Identity;
using LedKasa.Siparis.Security;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace LedKasa.Siparis.Tests.Security;

public class CurrentUserTests
{
    [Fact]
    public void CanCreateOrders_ShouldUseCircuitUser_WhenHttpContextMissing()
    {
        var principal = Principal(AppRoles.SiparisPersoneli, "u-1");
        using var sut = new CurrentUser(new NullHttp(), new FixedAuth(principal));

        sut.CanCreateOrders.Should().BeTrue();
        sut.UserId.Should().Be("u-1");
        sut.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void UserId_ShouldThrow_WhenAnonymous()
    {
        using var sut = new CurrentUser(new NullHttp(), new FixedAuth(new ClaimsPrincipal(new ClaimsIdentity())));
        var act = () => _ = sut.UserId;
        act.Should().Throw<InvalidOperationException>();
    }

    private static ClaimsPrincipal Principal(string role, string userId) =>
        new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role),
            new Claim("display_name", "Test")
        ], "test"));

    private sealed class NullHttp : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }

    private sealed class FixedAuth(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(user));
    }
}
