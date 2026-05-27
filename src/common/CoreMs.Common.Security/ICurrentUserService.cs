namespace CoreMs.Common.Security;

/// <summary>
/// Provides access to the currently authenticated user's identity claims.
/// </summary>
public interface ICurrentUserService
{
    Guid GetCurrentUserUuid();
    string GetCurrentUserEmail();
    IReadOnlyList<string> GetCurrentUserRoles();
    bool IsInRole(string role);
}
