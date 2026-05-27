---
inclusion: fileMatch
fileMatchPattern: "**/{Clients,HttpClients}/**/*.cs"
---

# Service Integration Guide (.NET)

## Overview
This guide explains how to integrate one microservice with another in the CoreMS .NET project using typed HTTP clients.

## Steps to Connect Services

### 1. Create a Typed HttpClient Interface

Define the client contract in the calling service's Domain layer:

```csharp
// In CoreMs.CommunicationMs.Domain/Interfaces/ITemplateMsClient.cs
public interface ITemplateMsClient
{
    Task<RenderedTemplateResponse> RenderTemplateAsync(
        string templateId, Dictionary<string, object> parameters, CancellationToken ct = default);
}
```

### 2. Implement the Typed HttpClient

```csharp
// In CoreMs.CommunicationMs.Infrastructure/HttpClients/TemplateMsClient.cs
public class TemplateMsClient : ITemplateMsClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TemplateMsClient> _logger;

    public TemplateMsClient(HttpClient httpClient, ILogger<TemplateMsClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<RenderedTemplateResponse> RenderTemplateAsync(
        string templateId, Dictionary<string, object> parameters, CancellationToken ct = default)
    {
        var request = new RenderTemplateRequest { TemplateId = templateId, Parameters = parameters };
        var response = await _httpClient.PostAsJsonAsync("/api/templates/render", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RenderedTemplateResponse>(ct))!;
    }
}
```

### 3. Register in DI (Program.cs or Extension Method)

```csharp
// In Program.cs or a ServiceCollectionExtensions class
builder.Services.AddHttpClient<ITemplateMsClient, TemplateMsClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:TemplateMs"]
        ?? "http://localhost:5004");
});
```

### 4. Configure Base URL in appsettings.json

```json
{
  "ServiceUrls": {
    "TemplateMs": "http://localhost:5004",
    "UserMs": "http://localhost:5000",
    "CommunicationMs": "http://localhost:5001",
    "DocumentMs": "http://localhost:5002",
    "TranslationMs": "http://localhost:5003"
  }
}
```

### 5. Configure Docker Compose

```yaml
environment:
  - ServiceUrls__TemplateMs=http://template-ms:5004
```

### 6. Use in Service Layer

```csharp
public class EmailService
{
    private readonly ITemplateMsClient _templateClient;

    public EmailService(ITemplateMsClient templateClient)
    {
        _templateClient = templateClient;
    }

    public async Task SendWelcomeEmail(string email, string firstName)
    {
        var rendered = await _templateClient.RenderTemplateAsync(
            "welcome-email",
            new Dictionary<string, object> { ["firstName"] = firstName });

        // Send email with rendered.Content
    }
}
```

## Port Reference

| Service | Port |
|---------|------|
| User Service | 5000 |
| Communication Service | 5001 |
| Document Service | 5002 |
| Translation Service | 5003 |
| Template Service | 5004 |

## Error Handling

```csharp
public async Task<RenderedTemplateResponse> RenderTemplateAsync(
    string templateId, Dictionary<string, object> parameters, CancellationToken ct = default)
{
    try
    {
        var response = await _httpClient.PostAsJsonAsync("/api/templates/render", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RenderedTemplateResponse>(ct))!;
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "Failed to call Template service for template {TemplateId}", templateId);
        throw ServiceException.Of(
            CommunicationExceptionCodes.ExternalServiceError,
            $"Failed to render template: {ex.Message}");
    }
}
```

## Resilience (Polly)

Add retry and circuit breaker policies:

```csharp
builder.Services.AddHttpClient<ITemplateMsClient, TemplateMsClient>(client =>
{
    client.BaseAddress = new Uri(config["ServiceUrls:TemplateMs"]!);
})
.AddStandardResilienceHandler(); // Microsoft.Extensions.Http.Resilience
```

## Testing

Mock the client interface in unit tests:

```csharp
var mockClient = new Mock<ITemplateMsClient>();
mockClient.Setup(c => c.RenderTemplateAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), default))
    .ReturnsAsync(new RenderedTemplateResponse { Content = "<html>Hello</html>" });

var service = new EmailService(mockClient.Object);
```

For integration tests, use `WireMock.Net` to stub external service responses:

```csharp
var server = WireMockServer.Start();
server.Given(Request.Create().WithPath("/api/templates/render").UsingPost())
    .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new { Content = "rendered" }));
```
