using CoreMs.Common.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CoreMs.Common.Http;

public static class HttpClientExtensions
{
    /// <summary>
    /// Registers a typed HttpClient for service-to-service communication using Aspire service discovery.
    /// Automatically handles auth: forwards JWT from HttpContext, or mints a fresh token from ServiceCallContext.
    ///
    /// The serviceName is the Aspire resource name (e.g., "communication-ms") which gets resolved
    /// by service discovery at runtime.
    ///
    /// Usage in Program.cs:
    ///   builder.AddCoreMsClient&lt;CommunicationMsClient&gt;("communication-ms");
    /// </summary>
    public static IHostApplicationBuilder AddCoreMsClient<TClient>(
        this IHostApplicationBuilder builder,
        string serviceName) where TClient : class
    {
        RegisterSharedDependencies(builder.Services, builder.Configuration);

        builder.Services.AddHttpClient<TClient>(client =>
            {
                client.BaseAddress = new Uri($"http://{serviceName}");
                client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddHttpMessageHandler<ServiceAuthDelegatingHandler>();

        return builder;
    }

    /// <summary>
    /// Registers a typed HttpClient for service-to-service communication with an explicit base URL.
    /// Automatically handles auth: forwards JWT from HttpContext, or mints a fresh token from ServiceCallContext.
    ///
    /// Use this variant when not using Aspire service discovery (e.g., in tests or standalone mode).
    ///
    /// Usage:
    ///   builder.Services.AddCoreMsHttpClient&lt;CommunicationMsClient&gt;("http://localhost:5101");
    /// </summary>
    public static IHttpClientBuilder AddCoreMsHttpClient<TClient>(
        this IServiceCollection services,
        string baseUrl) where TClient : class
    {
        services.AddScoped<ServiceCallContext>();
        services.AddScoped<ServiceAuthDelegatingHandler>();

        return services.AddHttpClient<TClient>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddHttpMessageHandler<ServiceAuthDelegatingHandler>();
    }

    private static void RegisterSharedDependencies(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        services.AddScoped<ServiceCallContext>();
        services.AddScoped<ServiceAuthDelegatingHandler>();

        // Ensure TokenProvider is registered (idempotent — Configure + AddSingleton are safe to call multiple times)
        services.Configure<TokenProviderOptions>(configuration.GetSection(TokenProviderOptions.SectionName));
        services.TryAddSingleton<TokenProvider>();
    }
}
