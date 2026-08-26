using CoreMs.Common.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CoreMs.UserMs.Client;

public static class UserMsClientExtensions
{
    /// <summary>
    /// Registers the UserMsClient using Aspire service discovery.
    ///
    /// Usage in Program.cs:
    ///   builder.AddUserMsClient();
    /// </summary>
    public static IHostApplicationBuilder AddUserMsClient(
        this IHostApplicationBuilder builder,
        string serviceName = "user-ms")
    {
        builder.AddCoreMsClient<UserMsClient>(serviceName);
        return builder;
    }

    /// <summary>
    /// Registers the UserMsClient with an explicit base URL (no service discovery).
    /// </summary>
    public static IServiceCollection AddUserMsClient(this IServiceCollection services, string baseUrl)
    {
        services.AddCoreMsHttpClient<UserMsClient>(baseUrl);
        return services;
    }
}
