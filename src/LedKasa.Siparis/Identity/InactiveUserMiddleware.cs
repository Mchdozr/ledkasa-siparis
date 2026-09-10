using Microsoft.AspNetCore.Identity;

namespace LedKasa.Siparis.Identity;

public sealed class InactiveUserMiddleware
{
    private readonly RequestDelegate _next;

    public InactiveUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var user = await users.GetUserAsync(context.User);
            if (user is { IsActive: false })
            {
                await signIn.SignOutAsync();
                context.Response.Redirect("/login?inactive=1");
                return;
            }
        }

        await _next(context);
    }
}
