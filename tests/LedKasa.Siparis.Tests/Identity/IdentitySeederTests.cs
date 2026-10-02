using FluentAssertions;
using LedKasa.Siparis.Identity;
using LedKasa.Siparis.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;

namespace LedKasa.Siparis.Tests.Identity;

public class IdentitySeederTests
{
    [Fact]
    public async Task EnsureBootstrapAdmin_ShouldCreateActiveAdmin_WhenMissing()
    {
        await using var db = TestDb.Create();
        var (users, _, _) = TestIdentity.Create(db);

        await IdentitySeeder.EnsureBootstrapAdminAsync(users, NullLogger.Instance);

        var user = await users.FindByNameAsync("Mchdozr");
        user.Should().NotBeNull();
        user!.IsActive.Should().BeTrue();
        user.DisplayName.Should().Be("Mchdozr");
        user.SecurityStamp.Should().NotBeNullOrEmpty();
        (await users.IsInRoleAsync(user, AppRoles.Yonetici)).Should().BeTrue();
        (await users.CheckPasswordAsync(user, "M715020m")).Should().BeTrue();
        (await users.CheckPasswordAsync(user, "yanlis")).Should().BeFalse();
    }

    [Fact]
    public void BootstrapAdminHash_ShouldVerifyPassword()
    {
        var hasher = new PasswordHasher<ApplicationUser>();

        hasher.VerifyHashedPassword(new ApplicationUser(), IdentitySeeder.BootstrapAdminPasswordHash, "M715020m")
            .Should().Be(PasswordVerificationResult.Success);
    }

    [Fact]
    public async Task EnsureBootstrapAdmin_ShouldNotOverwriteExistingUser()
    {
        await using var db = TestDb.Create();
        var (users, _, _) = TestIdentity.Create(db);
        var existing = new ApplicationUser { UserName = "Mchdozr", DisplayName = "Özel Ad", IsActive = false };
        (await users.CreateAsync(existing, "baska")).Succeeded.Should().BeTrue();
        var hashBefore = existing.PasswordHash;

        await IdentitySeeder.EnsureBootstrapAdminAsync(users, NullLogger.Instance);
        await IdentitySeeder.EnsureBootstrapAdminAsync(users, NullLogger.Instance);

        users.Users.Count(u => u.UserName == "Mchdozr").Should().Be(1);
        var user = await users.FindByNameAsync("Mchdozr");
        user!.PasswordHash.Should().Be(hashBefore);
        user.DisplayName.Should().Be("Özel Ad");
        user.IsActive.Should().BeFalse();
        (await users.IsInRoleAsync(user, AppRoles.Yonetici)).Should().BeFalse();
    }

    [Fact]
    public async Task EnsureBootstrapAdmin_ShouldBeIdempotent()
    {
        await using var db = TestDb.Create();
        var (users, _, _) = TestIdentity.Create(db);

        await IdentitySeeder.EnsureBootstrapAdminAsync(users, NullLogger.Instance);
        await IdentitySeeder.EnsureBootstrapAdminAsync(users, NullLogger.Instance);

        users.Users.Count(u => u.UserName == "Mchdozr").Should().Be(1);
    }
}
