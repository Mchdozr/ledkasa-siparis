using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace LedKasa.Siparis.Features.Orders;

public static class OrdersModule
{
    public static IServiceCollection AddOrdersModule(this IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();
        services.AddValidatorsFromAssemblyContaining<OrderDraftValidator>();
        return services;
    }
}
