namespace LedKasa.Siparis.Features.Orders.Domain;

public static class DisplayNames
{
    public static string Status(OrderStatus status) => status switch
    {
        OrderStatus.Yeni => "Yeni",
        OrderStatus.Onaylandi => "Onaylandı",
        OrderStatus.Uretimde => "Üretimde",
        OrderStatus.Hazir => "Hazır",
        OrderStatus.TeslimEdildi => "Teslim Edildi",
        OrderStatus.Iptal => "İptal",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    public static string Place(DeliveryPlace place) => place switch
    {
        DeliveryPlace.Sirket => "Şirket",
        DeliveryPlace.Fabrika => "Fabrika",
        _ => throw new ArgumentOutOfRangeException(nameof(place), place, null)
    };
}
