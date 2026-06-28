using Microsoft.Extensions.DependencyInjection;

namespace CoreMs.Common.Http;

public static class HttpClientExtensions
{
    /// <summary>
    /// Registers a typed HttpClient for service-to-service communication.
    /// Automatically forwards JWT token and correlation ID from the incoming request.
    ///
    /// Usage:
    ///   builder.Services.AddCoreMsHttpClient&lt;ICommunicationMsApi&gt;(baseUrl);
    /// </summary>
    public static IHttpClientBuilder AddCoreMsHttpClient<TClient>(
        this IServiceCollection services,
        string baseUrl) where TClient : class
    {
        services.AddTransient<ServiceAuthDelegatingHandler>();

        return services.AddHttpClient<TClient>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddHttpMessageHandler<ServiceAuthDelegatingHandler>();
    }

    /// <summary>
    /// Registers a typed HttpClient with URL resolved from configuration.
    /// </summary>
    public static IHttpClientBuilder AddCoreMsHttpClient<TClient>(
        this IServiceCollection services,
        Uri baseUri) where TClient : class
    {
        services.AddTransient<ServiceAuthDelegatingHandler>();

        return services.AddHttpClient<TClient>(client =>
            {
                client.BaseAddress = baseUri;
                client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddHttpMessageHandler<ServiceAuthDelegatingHandler>();
    }
}
