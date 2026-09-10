using LedKasa.Siparis.Features.Orders.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LedKasa.Siparis.Identity;

public sealed class UserAdminDto
{
    public string Id { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public sealed class CreateUserRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = AppRoles.SiparisPersoneli;
}

public interface IUserAdminService
{
    Task<IReadOnlyList<UserAdminDto>> ListAsync(CancellationToken cancellationToken = default);
    Task CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task SetActiveAsync(string userId, bool isActive, CancellationToken cancellationToken = default);
    Task SetRoleAsync(string userId, string role, CancellationToken cancellationToken = default);
}

public sealed class UserAdminService : IUserAdminService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<IdentityRole> _roles;

    public UserAdminService(UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles)
    {
        _users = users;
        _roles = roles;
    }

    public async Task<IReadOnlyList<UserAdminDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var users = await _users.Users.OrderBy(u => u.DisplayName).ToListAsync(cancellationToken);
        var result = new List<UserAdminDto>(users.Count);
        foreach (var user in users)
        {
            var roles = await _users.GetRolesAsync(user);
            result.Add(new UserAdminDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName,
                Role = roles.FirstOrDefault() ?? string.Empty,
                IsActive = user.IsActive
            });
        }

        return result;
    }

    public async Task CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (!AppRoles.All.Contains(request.Role))
            throw new DomainException("Geçersiz rol.");

        if (!await _roles.RoleExistsAsync(request.Role))
            throw new DomainException("Rol tanımlı değil.");

        var userName = FirstNonEmpty(request.UserName, request.Email);
        if (string.IsNullOrWhiteSpace(userName))
            throw new DomainException("Kullanıcı adı zorunludur.");
        if (string.IsNullOrWhiteSpace(request.Password))
            throw new DomainException("Şifre zorunludur.");

        var email = userName.Contains('@') ? userName : null;
        var displayName = FirstNonEmpty(request.DisplayName, userName);

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            DisplayName = displayName,
            EmailConfirmed = email is not null,
            IsActive = true
        };

        var created = await _users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
            throw new DomainException(string.Join(" ", created.Errors.Select(e => e.Description)));

        var roleResult = await _users.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
            throw new DomainException(string.Join(" ", roleResult.Errors.Select(e => e.Description)));
    }

    public async Task SetActiveAsync(string userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByIdAsync(userId)
            ?? throw new DomainException("Kullanıcı bulunamadı.");
        user.IsActive = isActive;
        if (!isActive)
            await _users.UpdateSecurityStampAsync(user);
        await _users.UpdateAsync(user);
    }

    public async Task SetRoleAsync(string userId, string role, CancellationToken cancellationToken = default)
    {
        if (!AppRoles.All.Contains(role))
            throw new DomainException("Geçersiz rol.");

        var user = await _users.FindByIdAsync(userId)
            ?? throw new DomainException("Kullanıcı bulunamadı.");

        var current = await _users.GetRolesAsync(user);
        if (current.Count > 0)
            await _users.RemoveFromRolesAsync(user, current);
        await _users.AddToRoleAsync(user, role);
    }

    private static string FirstNonEmpty(string first, string second)
    {
        if (!string.IsNullOrWhiteSpace(first))
            return first.Trim();
        return string.IsNullOrWhiteSpace(second) ? string.Empty : second.Trim();
    }
}
