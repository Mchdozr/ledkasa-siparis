using Microsoft.Extensions.DependencyInjection;

namespace LedKasa.Siparis.Identity;

public static class UsersModule
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services)
    {
        services.AddScoped<IUserAdminService, UserAdminService>();
        return services;
    }
}
