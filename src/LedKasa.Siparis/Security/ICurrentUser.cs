using LedKasa.Siparis.Identity;

namespace LedKasa.Siparis.Security;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    string UserId { get; }
    string? DisplayName { get; }
    string? UserName { get; }
    bool CanViewPrices { get; }
}

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _http;

    public CurrentUser(IHttpContextAccessor http)
    {
        _http = http;
    }

    public bool IsAuthenticated => _http.HttpContext?.User.Identity?.IsAuthenticated == true;
    public string UserId => _http.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? throw new InvalidOperationException("Oturum kullanıcı kimliği bulunamadı.");
    public string? DisplayName => _http.HttpContext?.User.FindFirst("display_name")?.Value
        ?? _http.HttpContext?.User.Identity?.Name;
    public string? UserName => _http.HttpContext?.User.Identity?.Name;

    public bool CanViewPrices
    {
        get
        {
            var user = _http.HttpContext?.User;
            return user is not null &&
                   (user.IsInRole(AppRoles.Yonetici) || user.IsInRole(AppRoles.Muhasebe));
        }
    }
}
