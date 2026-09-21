namespace LedKasa.Siparis.Security;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    string UserId { get; }
    string? DisplayName { get; }
    string? UserName { get; }
    bool CanCreateOrders { get; }
    bool CanDeleteOrders { get; }
}
