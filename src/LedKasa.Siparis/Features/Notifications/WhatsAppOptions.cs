namespace LedKasa.Siparis.Features.Notifications;

public sealed class WhatsAppOptions
{
    public const string Section = "WhatsApp";

    public string? AccessToken { get; set; }
    public string? PhoneNumberId { get; set; }
    public string? Recipients { get; set; }
    public string AppUrl { get; set; } = "https://siparis.ledkasa.com.tr";
    public string GraphApiVersion { get; set; } = "v21.0";
    public string? TemplateName { get; set; }
    public string TemplateLanguage { get; set; } = "tr";

    public string? TrimmedToken => BlankToNull(AccessToken);
    public string? TrimmedPhoneNumberId => BlankToNull(PhoneNumberId);
    public string? TrimmedTemplateName => BlankToNull(TemplateName);
    public string TrimmedApiVersion => BlankToNull(GraphApiVersion) ?? "v21.0";
    public string TrimmedTemplateLanguage => BlankToNull(TemplateLanguage) ?? "tr";

    private static string? BlankToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
