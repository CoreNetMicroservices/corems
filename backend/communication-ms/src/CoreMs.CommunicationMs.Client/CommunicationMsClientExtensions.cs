using CoreMs.Common.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CoreMs.CommunicationMs.Client;

public static class CommunicationMsClientExtensions
{
    /// <summary>
    /// Registers the CommunicationMsClient using Aspire service discovery.
    ///
    /// Usage in Program.cs:
    ///   builder.AddCommunicationMsClient();
    /// </summary>
    public static IHostApplicationBuilder AddCommunicationMsClient(
        this IHostApplicationBuilder builder,
        string serviceName = "communication-ms")
    {
        builder.AddCoreMsClient<CommunicationMsClient>(serviceName);
        return builder;
    }

    /// <summary>
    /// Registers the CommunicationMsClient with an explicit base URL (no service discovery).
    /// </summary>
    public static IServiceCollection AddCommunicationMsClient(this IServiceCollection services, string baseUrl)
    {
        services.AddCoreMsHttpClient<CommunicationMsClient>(baseUrl);
        return services;
    }
}
