using FluentAssertions;
using LedKasa.Siparis.Features.Orders.Domain;
using LedKasa.Siparis.Identity;
using LedKasa.Siparis.Tests.Infrastructure;

namespace LedKasa.Siparis.Tests.Identity;

public class UserAdminServiceTests
{
    [Fact]
    public async Task UpdateCredentials_ShouldChangeUserNameAndPassword_WithoutStoringPlaintext()
    {
        await using var db = TestDb.Create();
        var (users, _, api) = TestIdentity.Create(db);
        var id = await CreateUserAsync(api, "personel1", "eski");

        await api.UpdateCredentialsAsync(new UpdateUserCredentialsRequest
        {
            UserId = id,
            UserName = "personel1.yeni",
            Password = "x"
        });

        var updated = await users.FindByIdAsync(id);
        updated!.UserName.Should().Be("personel1.yeni");
        updated.PasswordHash.Should().NotBeNull();
        updated.PasswordHash.Should().NotBe("x");
        (await users.CheckPasswordAsync(updated, "x")).Should().BeTrue();
        (await users.CheckPasswordAsync(updated, "eski")).Should().BeFalse();
        (await users.FindByNameAsync("personel1")).Should().BeNull();
    }

    [Fact]
    public async Task UpdateCredentials_ShouldChangeOwnEmail()
    {
        await using var db = TestDb.Create();
        var (users, _, api) = TestIdentity.Create(db);
        var id = await CreateUserAsync(api, "admin@ledkasa.com.tr", "eski");

        await api.UpdateCredentialsAsync(new UpdateUserCredentialsRequest
        {
            UserId = id,
            UserName = "yonetici",
            Email = "yeni@ledkasa.com.tr",
            Password = "yeni"
        });

        var updated = await users.FindByIdAsync(id);
        updated!.UserName.Should().Be("yonetici");
        updated.Email.Should().Be("yeni@ledkasa.com.tr");
        (await users.FindByEmailAsync("yeni@ledkasa.com.tr")).Should().NotBeNull();
        (await users.CheckPasswordAsync(updated, "yeni")).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCredentials_EmptyPassword_ShouldKeepExistingPassword()
    {
        await using var db = TestDb.Create();
        var (users, _, api) = TestIdentity.Create(db);
        var id = await CreateUserAsync(api, "ali", "kalacak");

        await api.UpdateCredentialsAsync(new UpdateUserCredentialsRequest
        {
            UserId = id,
            UserName = "ali.yeni",
            Password = "   "
        });

        var updated = await users.FindByIdAsync(id);
        (await users.CheckPasswordAsync(updated!, "kalacak")).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCredentials_ShouldRejectDuplicateUserName()
    {
        await using var db = TestDb.Create();
        var (_, _, api) = TestIdentity.Create(db);
        await CreateUserAsync(api, "mevcut", "a");
        var otherId = await CreateUserAsync(api, "diger", "b");

        var act = async () => await api.UpdateCredentialsAsync(new UpdateUserCredentialsRequest
        {
            UserId = otherId,
            UserName = "mevcut",
            Password = "c"
        });

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*kullanıcı adı*");
    }

    [Fact]
    public async Task Create_ShouldRejectDuplicateUserName()
    {
        await using var db = TestDb.Create();
        var (_, _, api) = TestIdentity.Create(db);
        await CreateUserAsync(api, "mevcut", "a");

        var act = async () => await CreateUserAsync(api, "mevcut", "b");
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*kullanıcı adı*");
    }

    [Fact]
    public async Task UpdateCredentials_ShouldRejectEmptyUserName()
    {
        await using var db = TestDb.Create();
        var (_, _, api) = TestIdentity.Create(db);
        var id = await CreateUserAsync(api, "ali", "a");

        var act = async () => await api.UpdateCredentialsAsync(new UpdateUserCredentialsRequest
        {
            UserId = id,
            UserName = "  "
        });

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Kullanıcı adı*");
    }

    [Fact]
    public async Task UpdateCredentials_ShouldRejectUnknownUser()
    {
        await using var db = TestDb.Create();
        var (_, _, api) = TestIdentity.Create(db);

        var act = async () => await api.UpdateCredentialsAsync(new UpdateUserCredentialsRequest
        {
            UserId = "yok",
            UserName = "ali"
        });

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*bulunamadı*");
    }

    private static async Task<string> CreateUserAsync(UserAdminService api, string userName, string password)
    {
        await api.CreateAsync(new CreateUserRequest
        {
            UserName = userName,
            Password = password,
            Role = AppRoles.SiparisPersoneli
        });
        var list = await api.ListAsync();
        return list.Single(u => u.UserName == userName).Id;
    }
}
