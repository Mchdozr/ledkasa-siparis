namespace LedKasa.Siparis.Identity;

public static class AppRoles
{
    public const string Yonetici = "Yonetici";
    public const string SiparisPersoneli = "SiparisPersoneli";
    public const string Muhasebe = "Muhasebe";

    public static readonly string[] All = [Yonetici, SiparisPersoneli, Muhasebe];

    public static string DisplayName(string role) => role switch
    {
        Yonetici => "Yönetici",
        SiparisPersoneli => "Sipariş Personeli",
        Muhasebe => "Muhasebe",
        _ => role
    };
}
