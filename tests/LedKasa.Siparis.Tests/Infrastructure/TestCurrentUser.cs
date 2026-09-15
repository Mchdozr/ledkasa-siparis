using LedKasa.Siparis.Security;

namespace LedKasa.Siparis.Tests.Infrastructure;

internal sealed class TestCurrentUser : ICurrentUser
{
    public bool IsAuthenticated => true;
    public string UserId { get; init; } = "user-1";
    public string? DisplayName { get; init; } = "Test";
    public string? UserName { get; init; } = "test@ledkasa.com.tr";
    public bool CanCreateOrders { get; init; } = true;
}
