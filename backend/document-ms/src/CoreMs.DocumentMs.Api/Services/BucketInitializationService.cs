using CoreMs.DocumentMs.Core.Services;

namespace CoreMs.DocumentMs.Api.Services;

public class BucketInitializationService(IServiceScopeFactory scopeFactory, ILogger<BucketInitializationService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
            await storageService.EnsureContainerExistsAsync(cancellationToken);
            logger.LogInformation("Storage container initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to initialize storage container. Service cannot accept requests");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
