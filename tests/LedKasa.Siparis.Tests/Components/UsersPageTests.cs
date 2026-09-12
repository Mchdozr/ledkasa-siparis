using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using LedKasa.Siparis.Components.Pages.Admin;
using LedKasa.Siparis.Identity;
using LedKasa.Siparis.Security;
using LedKasa.Siparis.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace LedKasa.Siparis.Tests.Components;

public class UsersPageTests : TestContext
{
    public UsersPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        var auth = this.AddTestAuthorization();
        auth.SetAuthorized("admin");
        auth.SetPolicies(Policies.AdminUsers);
        auth.SetRoles(AppRoles.Yonetici);
        Services.AddSingleton<ICurrentUser>(new TestCurrentUser { UserId = "user-1", UserName = "admin" });
        Services.AddSingleton<IUserAdminService>(new FakeUserAdmin());
    }

    [Fact]
    public void ShouldShowSelfCredentialsAndEdit()
    {
        var cut = RenderPage();
        cut.Markup.Should().Contain("Giriş bilgilerim");
        cut.Markup.Should().Contain("Düzenle");
        cut.Markup.Should().Contain("Siz");
    }

    [Fact]
    public void Edit_ShouldOpenCredentialsForm()
    {
        var cut = RenderPage();
        cut.WaitForAssertion(() =>
            cut.FindAll("button").Count(b => b.TextContent.Contains("Düzenle")).Should().BeGreaterThan(0));
        cut.FindAll("button").Last(b => b.TextContent.Contains("Düzenle")).Click();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Giriş bilgilerini düzenle"));
    }

    private IRenderedFragment RenderPage() => Render(builder =>
    {
        builder.OpenComponent<MudPopoverProvider>(0);
        builder.CloseComponent();
        builder.OpenComponent<MudSnackbarProvider>(1);
        builder.CloseComponent();
        builder.OpenComponent<Users>(2);
        builder.CloseComponent();
    });

    private sealed class FakeUserAdmin : IUserAdminService
    {
        public Task<IReadOnlyList<UserAdminDto>> ListAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<UserAdminDto>>(
            [
                new()
                {
                    Id = "user-1",
                    UserName = "admin",
                    Email = "admin@ledkasa.com.tr",
                    DisplayName = "Admin",
                    Role = AppRoles.Yonetici,
                    IsActive = true
                },
                new()
                {
                    Id = "user-2",
                    UserName = "ali",
                    Email = string.Empty,
                    DisplayName = "Ali",
                    Role = AppRoles.SiparisPersoneli,
                    IsActive = true
                }
            ]);

        public Task CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateCredentialsAsync(UpdateUserCredentialsRequest request, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SetActiveAsync(string userId, bool isActive, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SetRoleAsync(string userId, string role, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
