using CoreMs.UserMs.Domain.Interfaces;

namespace CoreMs.UserMs.Api.Services;

public class TokenCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TokenCleanupService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    public TokenCleanupService(IServiceScopeFactory scopeFactory, ILogger<TokenCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TokenCleanupService started. Running every {Interval}", Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(Interval, stoppingToken);

                using var scope = _scopeFactory.CreateScope();
                var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
                await tokenService.CleanupExpiredTokensAsync(stoppingToken);

                _logger.LogInformation("Token cleanup completed successfully");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token cleanup");
            }
        }

        _logger.LogInformation("TokenCleanupService stopped");
    }
}
