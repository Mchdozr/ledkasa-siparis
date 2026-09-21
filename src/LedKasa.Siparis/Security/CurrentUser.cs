using System.Security.Claims;
using LedKasa.Siparis.Identity;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace LedKasa.Siparis.Security;

public sealed class CurrentUser : ICurrentUser, IDisposable
{
    private readonly IHttpContextAccessor _http;
    private readonly AuthenticationStateProvider _auth;
    private ClaimsPrincipal _circuitUser = new(new ClaimsIdentity());

    public CurrentUser(IHttpContextAccessor http, AuthenticationStateProvider auth)
    {
        _http = http;
        _auth = auth;
        _auth.AuthenticationStateChanged += OnAuthenticationStateChanged;
        var initial = _auth.GetAuthenticationStateAsync();
        if (initial.IsCompletedSuccessfully)
            _circuitUser = initial.Result.User;
        else
            _ = PrimeAsync(initial);
    }

    public bool IsAuthenticated => User.Identity?.IsAuthenticated == true;

    public string UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new InvalidOperationException("Oturum kullanıcı kimliği bulunamadı.");

    public string? DisplayName => User.FindFirst("display_name")?.Value
        ?? User.Identity?.Name;

    public string? UserName => User.Identity?.Name;

    public bool CanCreateOrders
    {
        get
        {
            var user = User;
            if (user.Identity?.IsAuthenticated != true)
                return false;

            foreach (var role in AppRoles.All)
            {
                if (user.IsInRole(role) && Policies.ForRole(role).Contains(Policies.OrdersCreate))
                    return true;
            }

            return false;
        }
    }

    public bool CanDeleteOrders => Policies.CanDeleteOrders(User);

    public void Dispose() => _auth.AuthenticationStateChanged -= OnAuthenticationStateChanged;

    private ClaimsPrincipal User
    {
        get
        {
            var httpUser = _http.HttpContext?.User;
            if (httpUser?.Identity?.IsAuthenticated == true)
                return httpUser;
            return _circuitUser;
        }
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> task)
    {
        if (task.IsCompletedSuccessfully)
            _circuitUser = task.Result.User;
    }

    private async Task PrimeAsync(Task<AuthenticationState> task)
    {
        try
        {
            _circuitUser = (await task).User;
        }
        catch
        {
        }
    }
}
