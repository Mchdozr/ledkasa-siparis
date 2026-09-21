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
        _ => "—"
    };

    public static string Place(DeliveryPlace place) => place switch
    {
        DeliveryPlace.Sirket => "Şirket",
        DeliveryPlace.Fabrika => "Fabrika",
        _ => "—"
    };

    public static string Sort(OrderSort sort) => sort switch
    {
        OrderSort.NewestFirst => "Yeniden eskiye",
        OrderSort.OldestFirst => "Eskiden yeniye",
        OrderSort.NearestDelivery => "En yakın teslim",
        OrderSort.FarthestDelivery => "En uzak teslim",
        _ => "—"
    };

    public static string Side(PanelSide side) => side switch
    {
        PanelSide.TekYon => "Tek yön",
        PanelSide.CiftYon => "Çift yön",
        _ => "—"
    };

    public static string Size(int widthCm, int heightCm, int depthCm) =>
        $"{widthCm} × {heightCm} × {depthCm} cm";
}
