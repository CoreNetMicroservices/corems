using System.Collections.Concurrent;
using System.Text;
using CoreMs.Common.Testing;
using CoreMs.DocumentMs.Core.Entities;
using CoreMs.DocumentMs.Core.Enums;
using CoreMs.DocumentMs.Core.Services;
using CoreMs.DocumentMs.Infrastructure.Data;
using CoreMs.TemplateMs.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CoreMs.DocumentMs.IntegrationTests;

/// <summary>
/// WebApplicationFactory for document-ms integration tests. Uses SQLite (via CoreMsTestFactory),
/// in-memory storage (no MinIO/S3), and a stubbed template client.
/// </summary>
public class DocumentMsTestFactory : CoreMsTestFactory<Program, DocumentMsDbContext>
{
    public InMemoryStorageService Storage { get; } = new();
    public Guid SeededOwnerId { get; } = Guid.NewGuid();
    public Guid SeededDocUuid { get; private set; }

    private const string ObjectKey = "test-user/doc-object-key.txt";
    public static readonly byte[] SeededContent = Encoding.UTF8.GetBytes("Hello from document-ms integration test");

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Bucket"] = "test-bucket",
                ["Storage:UseAzureBlob"] = "false"
            });
        });
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Seed a document entity and its blob content
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DocumentMsDbContext>();

        SeededDocUuid = Guid.NewGuid();
        db.Set<DocumentEntity>().Add(new DocumentEntity
        {
            Uuid = SeededDocUuid,
            UserId = SeededOwnerId,
            Name = "Test Document",
            OriginalFilename = "test.txt",
            Size = SeededContent.Length,
            Extension = "txt",
            ContentType = "text/plain",
            Bucket = "test-bucket",
            ObjectKey = ObjectKey,
            Visibility = DocumentVisibility.Private,
            UploadedById = SeededOwnerId,
            UploadedByType = UploadedByType.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        Storage.Seed(ObjectKey, SeededContent);
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.RemoveAll<IStorageService>();
        services.AddSingleton<IStorageService>(Storage);

        services.RemoveAll<TemplateMsClient>();
        services.AddScoped(_ => new TemplateMsClient(
            new HttpClient(new StubHandler()) { BaseAddress = new Uri("http://template-stub") }));
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("""{"renderedContent":"<p>stub</p>"}""",
                    System.Text.Encoding.UTF8, "application/json")
            });
    }
}

/// <summary>Simple in-memory IStorageService for tests.</summary>
public sealed class InMemoryStorageService : IStorageService
{
    private readonly ConcurrentDictionary<string, byte[]> _store = new();

    public Task UploadAsync(Stream stream, string objectKey, string contentType, long size, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        _store[objectKey] = ms.ToArray();
        return Task.CompletedTask;
    }

    public Task<Stream> DownloadAsync(string objectKey, CancellationToken ct = default)
    {
        if (!_store.TryGetValue(objectKey, out var bytes))
            throw new FileNotFoundException($"Object not found: {objectKey}");
        return Task.FromResult<Stream>(new MemoryStream(bytes));
    }

    public Task DeleteAsync(string objectKey, CancellationToken ct = default) { _store.TryRemove(objectKey, out _); return Task.CompletedTask; }
    public Task<bool> ExistsAsync(string objectKey, CancellationToken ct = default) => Task.FromResult(_store.ContainsKey(objectKey));
    public Task EnsureContainerExistsAsync(CancellationToken ct = default) => Task.CompletedTask;
    public void Seed(string objectKey, byte[] content) => _store[objectKey] = content;
}
