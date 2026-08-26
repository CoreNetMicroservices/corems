using CoreMs.Common.Testing;
using CoreMs.TranslationMs.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CoreMs.TranslationMs.IntegrationTests;

/// <summary>
/// WebApplicationFactory for translation-ms integration tests. Boots the full service with
/// SQLite in place of Postgres, test auth handler, and runs the seeder for realistic data.
/// </summary>
public class TranslationMsTestFactory : CoreMsTestFactory<Program, TranslationMsDbContext>
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Run the seeder so tests have realistic translation data
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TranslationMsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger<SeedDataService>();
        await new SeedDataService(db, logger).SeedAsync();
    }
}
