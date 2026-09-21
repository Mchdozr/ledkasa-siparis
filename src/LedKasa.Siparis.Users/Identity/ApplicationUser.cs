using Microsoft.AspNetCore.Identity;

namespace LedKasa.Siparis.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
