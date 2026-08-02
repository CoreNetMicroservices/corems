using CoreMs.Common.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CoreMs.TemplateMs.Client;

public static class TemplateMsClientExtensions
{
    /// <summary>
    /// Registers the TemplateMsClient using Aspire service discovery.
    ///
    /// Usage in Program.cs:
    ///   builder.AddTemplateMsClient();
    /// </summary>
    public static IHostApplicationBuilder AddTemplateMsClient(
        this IHostApplicationBuilder builder,
        string serviceName = "template-ms")
    {
        builder.AddCoreMsClient<TemplateMsClient>(serviceName);
        return builder;
    }

    /// <summary>
    /// Registers the TemplateMsClient with an explicit base URL (no service discovery).
    /// </summary>
    public static IServiceCollection AddTemplateMsClient(this IServiceCollection services, string baseUrl)
    {
        services.AddCoreMsHttpClient<TemplateMsClient>(baseUrl);
        return services;
    }
}
