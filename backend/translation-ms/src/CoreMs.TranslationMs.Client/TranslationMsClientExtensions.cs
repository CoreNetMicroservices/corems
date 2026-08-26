using CoreMs.Common.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CoreMs.TranslationMs.Client;

public static class TranslationMsClientExtensions
{
    /// <summary>
    /// Registers the TranslationMsClient using Aspire service discovery.
    ///
    /// Usage in Program.cs:
    ///   builder.AddTranslationMsClient();
    /// </summary>
    public static IHostApplicationBuilder AddTranslationMsClient(
        this IHostApplicationBuilder builder,
        string serviceName = "translation-ms")
    {
        builder.AddCoreMsClient<TranslationMsClient>(serviceName);
        return builder;
    }

    /// <summary>
    /// Registers the TranslationMsClient with an explicit base URL (no service discovery).
    /// </summary>
    public static IServiceCollection AddTranslationMsClient(this IServiceCollection services, string baseUrl)
    {
        services.AddCoreMsHttpClient<TranslationMsClient>(baseUrl);
        return services;
    }
}
