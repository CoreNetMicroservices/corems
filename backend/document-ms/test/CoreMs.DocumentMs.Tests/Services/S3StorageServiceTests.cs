using Amazon.S3;
using Amazon.S3.Model;
using CoreMs.DocumentMs.Core.Configuration;
using CoreMs.DocumentMs.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace CoreMs.DocumentMs.Tests.Services;

public class S3StorageServiceTests
{
    private readonly IAmazonS3 _mockS3Client;
    private readonly S3StorageService _sut;
    private const string TestBucket = "test-bucket";

    public S3StorageServiceTests()
    {
        _mockS3Client = Substitute.For<IAmazonS3>();

        var options = Options.Create(new StorageOptions
        {
            Endpoint = "http://localhost:9000",
            AccessKey = "minioadmin",
            SecretKey = "minioadmin",
            Bucket = TestBucket,
            ForcePathStyle = true
        });

        var logger = Substitute.For<ILogger<S3StorageService>>();
        _sut = new S3StorageService(_mockS3Client, options, logger);
    }

    [Fact]
    public async Task UploadAsync_ShouldCallPutObjectAsync_WithCorrectParameters()
    {
        var content = new byte[] { 1, 2, 3, 4, 5 };
        using var stream = new MemoryStream(content);
        var objectKey = "uploads/test-file.pdf";
        var contentType = "application/pdf";
        var size = content.Length;

        _mockS3Client.PutObjectAsync(Arg.Any<PutObjectRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PutObjectResponse());

        await _sut.UploadAsync(stream, objectKey, contentType, size);

        await _mockS3Client.Received(1).PutObjectAsync(
            Arg.Is<PutObjectRequest>(r =>
                r.BucketName == TestBucket &&
                r.Key == objectKey &&
                r.ContentType == contentType &&
                r.InputStream == stream),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DownloadAsync_ShouldCallGetObjectAsync_AndReturnResponseStream()
    {
        var objectKey = "uploads/test-file.pdf";
        var expectedStream = new MemoryStream([10, 20, 30]);

        var response = new GetObjectResponse
        {
            ResponseStream = expectedStream
        };

        _mockS3Client.GetObjectAsync(Arg.Any<GetObjectRequest>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var result = await _sut.DownloadAsync(objectKey);

        result.Should().BeSameAs(expectedStream);
        await _mockS3Client.Received(1).GetObjectAsync(
            Arg.Is<GetObjectRequest>(r =>
                r.BucketName == TestBucket &&
                r.Key == objectKey),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ShouldCallDeleteObjectAsync_WithCorrectParameters()
    {
        var objectKey = "uploads/test-file.pdf";

        _mockS3Client.DeleteObjectAsync(Arg.Any<DeleteObjectRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteObjectResponse());

        await _sut.DeleteAsync(objectKey);

        await _mockS3Client.Received(1).DeleteObjectAsync(
            Arg.Is<DeleteObjectRequest>(r =>
                r.BucketName == TestBucket &&
                r.Key == objectKey),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenObjectExists()
    {
        var objectKey = "uploads/existing-file.pdf";

        _mockS3Client.GetObjectMetadataAsync(Arg.Any<GetObjectMetadataRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetObjectMetadataResponse());

        var result = await _sut.ExistsAsync(objectKey);

        result.Should().BeTrue();
        await _mockS3Client.Received(1).GetObjectMetadataAsync(
            Arg.Is<GetObjectMetadataRequest>(r =>
                r.BucketName == TestBucket &&
                r.Key == objectKey),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenNotFoundExceptionThrown()
    {
        var objectKey = "uploads/non-existent-file.pdf";

        var notFoundException = new AmazonS3Exception("Not Found")
        {
            StatusCode = System.Net.HttpStatusCode.NotFound
        };

        _mockS3Client.GetObjectMetadataAsync(Arg.Any<GetObjectMetadataRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(notFoundException);

        var result = await _sut.ExistsAsync(objectKey);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_ShouldRethrow_WhenNonNotFoundExceptionThrown()
    {
        var objectKey = "uploads/some-file.pdf";

        var serverException = new AmazonS3Exception("Internal Server Error")
        {
            StatusCode = System.Net.HttpStatusCode.InternalServerError
        };

        _mockS3Client.GetObjectMetadataAsync(Arg.Any<GetObjectMetadataRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(serverException);

        var act = async () => await _sut.ExistsAsync(objectKey);

        await act.Should().ThrowAsync<AmazonS3Exception>();
    }

    [Fact]
    public async Task EnsureContainerExistsAsync_ShouldCreateBucket_WhenItDoesNotExist()
    {
        var listResponse = new ListBucketsResponse
        {
            Buckets = [new S3Bucket { BucketName = "other-bucket" }]
        };

        _mockS3Client.ListBucketsAsync(Arg.Any<CancellationToken>())
            .Returns(listResponse);
        _mockS3Client.PutBucketAsync(Arg.Any<PutBucketRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PutBucketResponse());

        await _sut.EnsureContainerExistsAsync();

        await _mockS3Client.Received(1).PutBucketAsync(
            Arg.Is<PutBucketRequest>(r => r.BucketName == TestBucket),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureContainerExistsAsync_ShouldNotCreateBucket_WhenItAlreadyExists()
    {
        var listResponse = new ListBucketsResponse
        {
            Buckets = [new S3Bucket { BucketName = TestBucket }]
        };

        _mockS3Client.ListBucketsAsync(Arg.Any<CancellationToken>())
            .Returns(listResponse);

        await _sut.EnsureContainerExistsAsync();

        await _mockS3Client.DidNotReceive().PutBucketAsync(
            Arg.Any<PutBucketRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureContainerExistsAsync_ShouldRethrow_WhenListBucketsFails()
    {
        _mockS3Client.ListBucketsAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new AmazonS3Exception("Connection refused"));

        var act = async () => await _sut.EnsureContainerExistsAsync();

        await act.Should().ThrowAsync<AmazonS3Exception>();
    }
}
