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
        var sub = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User is not authenticated.");

        return Guid.Parse(sub);
    }

    public string GetCurrentUserEmail()
    {
        return _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email)
            ?? throw new InvalidOperationException("User is not authenticated.");
    }

    public IReadOnlyList<string> GetCurrentUserRoles()
    {
        var roles = _httpContextAccessor.HttpContext?.User
            .FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        return roles?.AsReadOnly() ?? new List<string>().AsReadOnly();
    }

    public bool IsInRole(string role)
    {
        return _httpContextAccessor.HttpContext?.User.IsInRole(role) ?? false;
    }
}
