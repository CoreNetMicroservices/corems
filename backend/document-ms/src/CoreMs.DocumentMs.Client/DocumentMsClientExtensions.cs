using CoreMs.Common.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CoreMs.DocumentMs.Client;

public static class DocumentMsClientExtensions
{
    /// <summary>
    /// Registers the DocumentMsClient using Aspire service discovery.
    /// </summary>
    public static IHostApplicationBuilder AddDocumentMsClient(
        this IHostApplicationBuilder builder,
        string serviceName = "document-ms")
    {
        builder.AddCoreMsClient<DocumentMsClient>(serviceName);
        return builder;
    }

    /// <summary>
    /// Registers the DocumentMsClient with an explicit base URL (no service discovery).
    /// </summary>
    public static IServiceCollection AddDocumentMsClient(this IServiceCollection services, string baseUrl)
    {
        services.AddCoreMsHttpClient<DocumentMsClient>(baseUrl);
        return services;
    }
}
