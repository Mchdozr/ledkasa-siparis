namespace LedKasa.Siparis.Features.Orders.Domain;

public static class OrderNumberFormatter
{
    public static string Format(DateOnly orderDate, int dailySequence)
    {
        if (dailySequence <= 0)
            throw new DomainException("Sipariş sıra numarası sıfırdan büyük olmalıdır.");

        return $"LK-{orderDate:yyyyMMdd}-{dailySequence:0000}";
    }
}
