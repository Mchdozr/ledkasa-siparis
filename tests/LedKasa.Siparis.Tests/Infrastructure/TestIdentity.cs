using LedKasa.Siparis.Data;
using LedKasa.Siparis.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LedKasa.Siparis.Tests.Infrastructure;

internal static class TestIdentity
{
    public static (UserManager<ApplicationUser> Users, RoleManager<IdentityRole> Roles, UserAdminService Api) Create(ApplicationDbContext db)
    {
        var lookup = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var options = Options.Create(new IdentityOptions
        {
            Password =
            {
                RequiredLength = 1,
                RequireDigit = false,
                RequireUppercase = false,
                RequireLowercase = false,
                RequireNonAlphanumeric = false,
                RequiredUniqueChars = 0
            },
            User =
            {
                RequireUniqueEmail = false,
                AllowedUserNameCharacters = string.Empty
            }
        });

        var users = new UserManager<ApplicationUser>(
            new UserStore<ApplicationUser, IdentityRole, ApplicationDbContext>(db),
            options,
            new PasswordHasher<ApplicationUser>(),
            [new UserValidator<ApplicationUser>()],
            [new PasswordValidator<ApplicationUser>()],
            lookup,
            errors,
            new DummyServiceProvider(),
            NullLogger<UserManager<ApplicationUser>>.Instance);

        var roles = new RoleManager<IdentityRole>(
            new RoleStore<IdentityRole, ApplicationDbContext>(db),
            [new RoleValidator<IdentityRole>()],
            lookup,
            errors,
            NullLogger<RoleManager<IdentityRole>>.Instance);

        foreach (var role in AppRoles.All)
            roles.CreateAsync(new IdentityRole(role)).GetAwaiter().GetResult();

        return (users, roles, new UserAdminService(users, roles));
    }

    private sealed class DummyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
