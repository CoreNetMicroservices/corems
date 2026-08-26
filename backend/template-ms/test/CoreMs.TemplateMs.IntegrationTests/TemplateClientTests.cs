using CoreMs.Common.Exceptions;
using CoreMs.TemplateMs.Client;
using FluentAssertions;
using Xunit;

namespace CoreMs.TemplateMs.IntegrationTests;

/// <summary>
/// Integration tests exercising template-ms through its own typed client.
/// Proves: routing, auth, serialization, Handlebars rendering, and error responses.
/// </summary>
public class TemplateClientTests : IClassFixture<TemplateMsTestFactory>
{
    private readonly TemplateMsClient _client;
    private readonly TemplateMsClient _anonymousClient;

    public TemplateClientTests(TemplateMsTestFactory factory)
    {
        // Authenticated client (render endpoint requires auth)
        var http = factory.CreateClientWithRoles("USER_MS_USER");
        _client = new TemplateMsClient(http);

        // Anonymous (no token) for auth rejection tests
        var anonHttp = factory.CreateAnonymousClient();
        _anonymousClient = new TemplateMsClient(anonHttp);
    }

    // ---- RenderTemplateAsync ----

    [Fact]
    public async Task RenderTemplate_SmsVerification_RendersWithParams()
    {
        var result = await _client.RenderTemplateAsync("sms-verification", new Dictionary<string, object>
        {
            ["code"] = "123456",
            ["expirationMinutes"] = "5"
        });

        result.Should().NotBeNull();
        result!.RenderedContent.Should().Contain("123456");
        result.RenderedContent.Should().Contain("5 minutes");
    }

    [Fact]
    public async Task RenderTemplate_EmailVerification_RendersHtml()
    {
        var result = await _client.RenderTemplateAsync("email-verification", new Dictionary<string, object>
        {
            ["firstName"] = "Alice",
            ["verificationUrl"] = "https://app.example.com/verify?token=abc",
            ["expirationHours"] = "24",
            ["year"] = "2026"
        });

        result.Should().NotBeNull();
        result!.RenderedContent.Should().Contain("Alice");
        result.RenderedContent.Should().Contain("https://app.example.com/verify?token=abc");
    }

    [Fact]
    public async Task RenderTemplate_UnknownTemplateId_ThrowsServiceException()
    {
        var act = async () => await _client.RenderTemplateAsync("nonexistent-template");

        await act.Should().ThrowAsync<ServiceException>();
    }

    [Fact]
    public async Task RenderTemplate_Anonymous_ThrowsUnauthorized()
    {
        var act = async () => await _anonymousClient.RenderTemplateAsync("sms-verification",
            new Dictionary<string, object> { ["code"] = "000000", ["expirationMinutes"] = "5" });

        await act.Should().ThrowAsync<ServiceException>();
    }

    // ---- GetTemplateMetadataAsync ----

    [Fact]
    public async Task GetTemplateMetadata_ExistingTemplate_ReturnsSchema()
    {
        var metadata = await _client.GetTemplateMetadataAsync("email-verification", "en");

        metadata.Should().NotBeNull();
        metadata!.TemplateId.Should().Be("email-verification");
        metadata.RequiredParameters.Should().Contain("firstName");
        metadata.RequiredParameters.Should().Contain("verificationUrl");
    }

    [Fact]
    public async Task GetTemplateMetadata_UnknownTemplate_ThrowsServiceException()
    {
        var act = async () => await _client.GetTemplateMetadataAsync("does-not-exist", "en");

        await act.Should().ThrowAsync<ServiceException>();
    }

    // ---- Round-trip correctness ----

    [Fact]
    public async Task RenderTemplate_InvoiceDocument_RendersScalarParams()
    {
        var result = await _client.RenderTemplateAsync("invoice-document", new Dictionary<string, object>
        {
            ["invoiceNumber"] = "INV-001",
            ["issueDate"] = "2026-08-01",
            ["dueDate"] = "2026-09-01",
            ["customerName"] = "Acme Corp",
            ["customerEmail"] = "billing@acme.com",
            ["items"] = new List<Dictionary<string, object>>
            {
                new() { ["description"] = "Consulting", ["amount"] = "$500" }
            },
            ["currency"] = "USD",
            ["totalAmount"] = "$2000",
            ["notes"] = "Thank you"
        });

        result.Should().NotBeNull();
        result!.RenderedContent.Should().Contain("INV-001");
        result.RenderedContent.Should().Contain("Acme Corp");
        result.RenderedContent.Should().Contain("USD");
        result.RenderedContent.Should().Contain("$2000");
    }
}
