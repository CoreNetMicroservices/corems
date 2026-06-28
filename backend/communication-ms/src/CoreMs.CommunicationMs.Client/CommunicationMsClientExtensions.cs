using CoreMs.Common.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CoreMs.CommunicationMs.Client;

public static class CommunicationMsClientExtensions
{
    /// <summary>
    /// Registers the CommunicationMsClient with service-to-service auth forwarding.
    ///
    /// Usage in any service's Program.cs:
    ///   builder.Services.AddCommunicationMsClient("http://communication-ms:5101");
    /// </summary>
    public static IServiceCollection AddCommunicationMsClient(this IServiceCollection services, string baseUrl)
    {
        services.AddCoreMsHttpClient<CommunicationMsClient>(baseUrl);
        return services;
    }
}
