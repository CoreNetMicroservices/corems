using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace CoreMs.Common.Security;

/// <summary>
/// Reads the current user's identity from HttpContext claims.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetCurrentUserUuid()
    {
        var sub = _httpContextAccessor.HttpContext?.User.FindFirstValue("sub")
            ?? _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User is not authenticated.");

        return Guid.Parse(sub);
    }

    public string GetCurrentUserEmail()
    {
        return _httpContextAccessor.HttpContext?.User.FindFirstValue("email")
            ?? _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email)
            ?? throw new InvalidOperationException("User is not authenticated.");
    }

    public IReadOnlyList<string> GetCurrentUserRoles()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user is null) return new List<string>().AsReadOnly();

        var roles = user.FindAll("role")
            .Concat(user.FindAll(ClaimTypes.Role))
            .Select(c => c.Value)
            .Distinct()
            .ToList();

        return roles.AsReadOnly();
    }

    public bool IsInRole(string role)
    {
        return _httpContextAccessor.HttpContext?.User.IsInRole(role) ?? false;
    }
}
