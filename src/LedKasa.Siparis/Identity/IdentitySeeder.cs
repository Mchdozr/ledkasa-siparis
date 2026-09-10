using LedKasa.Siparis.Features.Orders.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LedKasa.Siparis.Identity;

public sealed class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config, ILogger logger)
    {
        using var scope = services.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in AppRoles.All)
        {
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole(role));
        }

        var email = config["Seed:AdminEmail"] ?? "admin@ledkasa.com.tr";
        var password = config["Seed:AdminPassword"]
            ?? Environment.GetEnvironmentVariable("SEED__ADMINPASSWORD");

        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Yönetici hesabı oluşturulmadı: Seed:AdminPassword veya SEED__ADMINPASSWORD tanımlı değil.");
            return;
        }

        if (await users.FindByEmailAsync(email) is not null)
            return;

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = "LEDKASA Yönetici",
            IsActive = true
        };

        var result = await users.CreateAsync(admin, password);
        if (!result.Succeeded)
            throw new DomainException("Yönetici oluşturulamadı: " + string.Join(" ", result.Errors.Select(e => e.Description)));

        await users.AddToRoleAsync(admin, AppRoles.Yonetici);
        logger.LogInformation("Başlangıç yöneticisi oluşturuldu: {Email}", email);
    }
}
