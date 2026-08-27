using CoreMs.Common.Http;
using CoreMs.Common.Security;
using CoreMs.DocumentMs.Client;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CoreMs.DocumentMs.IntegrationTests;

/// <summary>
/// Integration tests exercising document-ms through its own typed client (DocumentMsClient).
/// Uses in-memory storage (no MinIO/S3).
/// </summary>
public class DocumentClientTests : IClassFixture<DocumentMsTestFactory>
{
    private readonly DocumentMsTestFactory _factory;
    private readonly DocumentMsClient _client;
    private readonly Guid _ownerId;
    private readonly Guid _docUuid;

    public DocumentClientTests(DocumentMsTestFactory factory)
    {
        _factory = factory;
        _ownerId = factory.SeededOwnerId;
        _docUuid = factory.SeededDocUuid;

        var http = factory.CreateClientWithRoles(_ownerId, "DOCUMENT_MS_USER");
        _client = new DocumentMsClient(http, new ServiceCallContext(), BuildTokenProvider());
    }

    private static TokenProvider BuildTokenProvider() =>
        new(Options.Create(new TokenProviderOptions
        {
            Algorithm = SigningAlgorithm.HS256,
            Issuer = "corems-test",
            Audience = "corems",
            SecretKey = "integration-test-secret-key-minimum-32-chars!"
        }));

    [Fact]
    public async Task GetDocumentMetadata_OwnedDocument_ReturnsMetadata()
    {
        var metadata = await _client.GetDocumentMetadataAsync(_docUuid);

        metadata.Should().NotBeNull();
        metadata!.Uuid.Should().Be(_docUuid);
        metadata.Name.Should().Be("Test Document");
        metadata.OriginalFilename.Should().Be("test.txt");
        metadata.Size.Should().Be(DocumentMsTestFactory.SeededContent.Length);
        metadata.Extension.Should().Be("txt");
    }

    [Fact]
    public async Task GetDocumentMetadata_UnknownDocument_ReturnsNull()
    {
        var metadata = await _client.GetDocumentMetadataAsync(Guid.NewGuid());
        metadata.Should().BeNull();
    }

    [Fact]
    public async Task DownloadDocument_OwnedDocument_StreamsContent()
    {
        using var download = await _client.DownloadDocumentAsync(_docUuid);

        download.Should().NotBeNull();
        download!.ContentType.Should().Be("text/plain");

        using var reader = new StreamReader(download.Stream);
        var text = await reader.ReadToEndAsync();
        text.Should().Be("Hello from document-ms integration test");
    }

    [Fact]
    public async Task DownloadDocument_UnknownDocument_ReturnsNull()
    {
        var download = await _client.DownloadDocumentAsync(Guid.NewGuid());
        download.Should().BeNull();
    }

    [Fact]
    public async Task GetDocumentMetadata_Anonymous_ReturnsNull()
    {
        var anonClient = new DocumentMsClient(
            _factory.CreateAnonymousClient(), new ServiceCallContext(), BuildTokenProvider());

        var metadata = await anonClient.GetDocumentMetadataAsync(_docUuid);
        metadata.Should().BeNull();
    }
}
