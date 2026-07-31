using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace CoreMs.Common.Security;

public static class AuthorizationExtensions
{
    /// <summary>
    /// Adds authorization with centralized role hierarchy handling.
    /// Use this instead of builder.Services.AddAuthorization() in Program.cs.
    /// </summary>
    public static IServiceCollection AddCoreMsAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization();
        services.AddSingleton<IAuthorizationHandler, CoreMsRolesAuthorizationHandler>();
        return services;
    }
}
