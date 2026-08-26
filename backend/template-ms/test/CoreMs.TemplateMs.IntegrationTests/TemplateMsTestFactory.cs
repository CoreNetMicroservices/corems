using CoreMs.Common.Testing;
using CoreMs.TemplateMs.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CoreMs.TemplateMs.IntegrationTests;

/// <summary>
/// WebApplicationFactory for template-ms integration tests. Boots the full service with
/// SQLite, test auth handler, and runs the seeder for realistic template data.
/// </summary>
public class TemplateMsTestFactory : CoreMsTestFactory<Program, TemplateMsDbContext>
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TemplateMsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger<SeedDataService>();
        await new SeedDataService(db, logger).SeedAsync();
    }
}
