namespace LedKasa.Siparis.Features.Orders.Domain;

public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
