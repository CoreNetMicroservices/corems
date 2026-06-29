namespace CoreMs.Common.Security;

/// <summary>
/// Role constants used across all CoreMS microservices for role-based access control.
/// </summary>
public static class CoreMsRoles
{
    public const string SuperAdmin = "SUPER_ADMIN";
    public const string UserMsAdmin = "USER_MS_ADMIN";
    public const string UserMsUser = "USER_MS_USER";
    public const string DocumentMsAdmin = "DOCUMENT_MS_ADMIN";
    public const string DocumentMsUser = "DOCUMENT_MS_USER";
}
