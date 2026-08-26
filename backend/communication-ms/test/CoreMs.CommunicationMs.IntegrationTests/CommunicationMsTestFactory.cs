using System.Net;
using CoreMs.Common.Http;
using CoreMs.Common.Security;
using CoreMs.Common.Testing;
using CoreMs.CommunicationMs.Infrastructure.Data;
using CoreMs.DocumentMs.Client;
using CoreMs.TemplateMs.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CoreMs.CommunicationMs.IntegrationTests;

/// <summary>
/// WebApplicationFactory for communication-ms integration tests. Boots the full service
/// with SQLite, test auth handler, and stubs external service clients (template-ms, document-ms).
/// </summary>
public class CommunicationMsTestFactory : CoreMsTestFactory<Program, CommunicationMsDbContext>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        // TokenProvider needs minimal JWT config to construct
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "integration-test-secret-key-minimum-32-chars!",
                ["Jwt:Issuer"] = "corems-test",
                ["Jwt:Algorithm"] = "HS256"
            });
        });
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        // Stub TemplateMsClient with a handler that returns a rendered template
        services.RemoveAll<TemplateMsClient>();
        services.AddScoped(_ => new TemplateMsClient(
            new HttpClient(new TemplateStubHandler()) { BaseAddress = new Uri("http://template-stub") }));

        // Stub DocumentMsClient with a no-op handler + real ServiceCallContext/TokenProvider from DI
        services.RemoveAll<DocumentMsClient>();
        services.AddScoped(sp => new DocumentMsClient(
            new HttpClient(new NoOpHandler()) { BaseAddress = new Uri("http://document-stub") },
            sp.GetRequiredService<ServiceCallContext>(),
            sp.GetRequiredService<TokenProvider>()));
    }

    /// <summary>Returns a rendered template response for any render request.</summary>
    private sealed class TemplateStubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var json = """{"renderedContent":"<p>Stubbed template content</p>"}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }

    /// <summary>Returns 200 OK with empty body for any request.</summary>
    private sealed class NoOpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });
        }
    }
}
