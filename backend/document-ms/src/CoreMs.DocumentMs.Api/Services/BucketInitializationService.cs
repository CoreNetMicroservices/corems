using CoreMs.DocumentMs.Core.Services;

namespace CoreMs.DocumentMs.Api.Services;

public class BucketInitializationService(IServiceScopeFactory scopeFactory, ILogger<BucketInitializationService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var storageService = scope.ServiceProvider.GetRequiredService<S3StorageService>();
            await storageService.EnsureBucketExistsAsync(cancellationToken);
            logger.LogInformation("Bucket initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to initialize storage bucket. Service cannot accept requests");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
