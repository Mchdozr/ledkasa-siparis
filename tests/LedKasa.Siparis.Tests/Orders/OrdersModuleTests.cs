using FluentAssertions;
using FluentValidation;
using LedKasa.Siparis.Features.Orders;
using Microsoft.Extensions.DependencyInjection;

namespace LedKasa.Siparis.Tests.Orders;

public class OrdersModuleTests
{
    [Fact]
    public void AddOrdersModule_ShouldRegisterOrderDraftValidator()
    {
        var services = new ServiceCollection();
        services.AddOrdersModule();

        using var provider = services.BuildServiceProvider();
        provider.GetService<IValidator<OrderDraft>>().Should().NotBeNull();
    }
}
