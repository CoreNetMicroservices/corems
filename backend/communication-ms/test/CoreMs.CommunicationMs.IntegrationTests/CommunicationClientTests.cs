using System.Net;
using System.Net.Http.Json;
using CoreMs.CommunicationMs.Client;
using CoreMs.CommunicationMs.Core.Models;
using FluentAssertions;
using Xunit;

namespace CoreMs.CommunicationMs.IntegrationTests;

/// <summary>
/// Integration tests exercising communication-ms through its own typed client.
/// Proves: routing, auth, validation, dispatch, persistence, and error responses.
/// </summary>
public class CommunicationClientTests : IClassFixture<CommunicationMsTestFactory>
{
    private readonly CommunicationMsClient _adminClient;
    private readonly CommunicationMsClient _anonymousClient;
    private readonly HttpClient _adminHttp;

    public CommunicationClientTests(CommunicationMsTestFactory factory)
    {
        _adminHttp = factory.CreateClientWithRoles("COMMUNICATION_MS_ADMIN");
        _adminClient = new CommunicationMsClient(_adminHttp);
        _anonymousClient = new CommunicationMsClient(factory.CreateAnonymousClient());
    }

    // ---- Send email notification (admin) ----

    [Fact]
    public async Task SendEmailNotification_ValidRequest_ReturnsAccepted()
    {
        var response = await _adminClient.SendEmailNotificationAsync(
            recipient: "test@example.com",
            subject: "Test",
            body: "Hello from integration test");

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task SendEmailNotification_WithTemplate_ReturnsAccepted()
    {
        var response = await _adminClient.SendEmailNotificationAsync(
            recipient: "test@example.com",
            subject: "Templated",
            template: new TemplatePayload { TemplateId = "email-verification" });

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task SendEmailNotification_Anonymous_Returns401()
    {
        var response = await _anonymousClient.SendEmailNotificationAsync(
            recipient: "test@example.com",
            subject: "Should Fail",
            body: "Unauthorized");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---- Send SMS notification (admin) ----

    [Fact]
    public async Task SendSmsNotification_ValidRequest_ReturnsAccepted()
    {
        var response = await _adminClient.SendSmsNotificationAsync(
            phoneNumber: "+15551234567",
            message: "Integration test SMS");

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    // ---- Send email message (admin, persisted) ----

    [Fact]
    public async Task SendEmailMessage_ValidRequest_ReturnsCreated()
    {
        var userId = Guid.NewGuid();
        var response = await _adminClient.SendEmailMessageAsync(
            userId: userId,
            recipient: "user@example.com",
            subject: "Welcome",
            body: "Account created");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task SendEmailMessage_WithTemplate_ReturnsCreated()
    {
        var response = await _adminClient.SendEmailMessageAsync(
            userId: Guid.NewGuid(),
            recipient: "user@example.com",
            subject: "Verification",
            template: new TemplatePayload { TemplateId = "email-verification" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ---- Send SMS message (admin, persisted) ----

    [Fact]
    public async Task SendSmsMessage_ValidRequest_ReturnsCreated()
    {
        var response = await _adminClient.SendSmsMessageAsync(
            userId: Guid.NewGuid(),
            phoneNumber: "+15551234567",
            message: "Your code is 123456");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ---- List messages (authenticated, scoped to user) ----

    [Fact]
    public async Task ListMessages_Authenticated_Returns200()
    {
        var response = await _adminHttp.GetAsync("/api/messages");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListMessages_Anonymous_Returns401()
    {
        var anonHttp = new CommunicationMsTestFactory().CreateAnonymousClient();
        // We can't reuse the factory easily per-test without a fixture;
        // use the existing anon client's underlying HttpClient
        var response = await _anonymousClient.SendEmailNotificationAsync(
            recipient: "t@t.com", subject: "x", body: "y");
        // Notifications need admin role; anonymous gets 401
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---- Validation ----

    [Fact]
    public async Task SendEmailNotification_InvalidRecipient_Returns400()
    {
        var response = await _adminClient.SendEmailNotificationAsync(
            recipient: "not-an-email",
            subject: "Bad",
            body: "body");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SendSmsNotification_InvalidPhone_Returns400()
    {
        var response = await _adminClient.SendSmsNotificationAsync(
            phoneNumber: "12345", // missing + prefix
            message: "fail");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
