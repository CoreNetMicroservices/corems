using System.Text.Json;
using CoreMs.TemplateMs.Core.Entities;
using CoreMs.TemplateMs.Infrastructure.Data;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace CoreMs.TemplateMs.Tests.Services;

/// <summary>
/// Property 11: Seed idempotence
/// For any number of times the seed operation is executed against the same database,
/// the resulting set of seed templates should be identical — no duplicates should be created.
///
/// **Validates: Requirements 11.3**
/// </summary>
public class SeedIdempotencePropertyTests
{
    private static (TestTemplateMsDbContext context, SqliteConnection connection) CreateDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<TemplateMsDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new TestTemplateMsDbContext(options);
        context.Database.EnsureCreated();
        return (context, connection);
    }

    [Property(MaxTest = 20, Arbitrary = [typeof(SeedRepetitionArbitraries)])]
    public void SeedAsync_CalledNTimes_AlwaysProducesSameCount(SeedRepetitionInput input)
    {
        var (context, connection) = CreateDbContext();
        try
        {
            var logger = Substitute.For<ILogger<SeedDataService>>();
            var seeder = new SeedDataService(context, logger);

            int? countAfterFirst = null;

            for (var i = 0; i < input.Repetitions; i++)
            {
                seeder.SeedAsync().GetAwaiter().GetResult();

                var currentCount = context.Set<TemplateEntity>().Count();

                if (countAfterFirst == null)
                {
                    countAfterFirst = currentCount;
                    countAfterFirst.Should().Be(9, "seed should create exactly 9 templates");
                }
                else
                {
                    currentCount.Should().Be(countAfterFirst.Value,
                        $"after {i + 1} seed executions, count should remain {countAfterFirst}");
                }
            }
        }
        finally
        {
            context.Dispose();
            connection.Close();
            connection.Dispose();
        }
    }

    [Property(MaxTest = 20, Arbitrary = [typeof(SeedRepetitionArbitraries)])]
    public void SeedAsync_CalledNTimes_NoDuplicateTemplateIds(SeedRepetitionInput input)
    {
        var (context, connection) = CreateDbContext();
        try
        {
            var logger = Substitute.For<ILogger<SeedDataService>>();
            var seeder = new SeedDataService(context, logger);

            for (var i = 0; i < input.Repetitions; i++)
            {
                seeder.SeedAsync().GetAwaiter().GetResult();
            }

            var templates = context.Set<TemplateEntity>().ToList();
            var uniqueKeys = templates.Select(t => $"{t.TemplateId}:{t.Language}").ToList();

            uniqueKeys.Should().OnlyHaveUniqueItems(
                "seed should never create duplicate (templateId, language) combinations");
        }
        finally
        {
            context.Dispose();
            connection.Close();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task SeedAsync_CreatesExpectedTemplates()
    {
        var (context, connection) = CreateDbContext();
        try
        {
            var logger = Substitute.For<ILogger<SeedDataService>>();
            var seeder = new SeedDataService(context, logger);

            await seeder.SeedAsync();

            var templates = await context.Set<TemplateEntity>().ToListAsync();

            templates.Should().HaveCount(9);
            templates.Should().Contain(t => t.TemplateId == "corems-styles");
            templates.Should().Contain(t => t.TemplateId == "email-verification");
            templates.Should().Contain(t => t.TemplateId == "welcome-email");
            templates.Should().Contain(t => t.TemplateId == "password-reset");
            templates.Should().Contain(t => t.TemplateId == "password-changed");
            templates.Should().Contain(t => t.TemplateId == "account-locked");
            templates.Should().Contain(t => t.TemplateId == "sms-verification");
            templates.Should().Contain(t => t.TemplateId == "sms-login-code");
            templates.Should().Contain(t => t.TemplateId == "invoice-document");
        }
        finally
        {
            context.Dispose();
            connection.Close();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task SeedAsync_AllTemplatesHaveValidContent()
    {
        var (context, connection) = CreateDbContext();
        try
        {
            var logger = Substitute.For<ILogger<SeedDataService>>();
            var seeder = new SeedDataService(context, logger);

            await seeder.SeedAsync();

            var templates = await context.Set<TemplateEntity>().ToListAsync();

            foreach (var template in templates)
            {
                template.Content.Should().NotBeNullOrEmpty();
                template.Name.Should().NotBeNullOrEmpty();
                template.Category.Should().NotBeNullOrEmpty();
                template.ParamSchema.Should().NotBeNull();
            }
        }
        finally
        {
            context.Dispose();
            connection.Close();
            connection.Dispose();
        }
    }
}

#region Test Infrastructure

/// <summary>
/// SQLite-compatible subclass of TemplateMsDbContext that replaces the jsonb column type
/// with a JSON string value converter for the ParamSchema property.
/// </summary>
public class TestTemplateMsDbContext : TemplateMsDbContext
{
    public TestTemplateMsDbContext(DbContextOptions<TemplateMsDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Skip base OnModelCreating (which applies PostgreSQL-specific configurations)
        // and configure a SQLite-compatible model instead
        modelBuilder.Entity<TemplateEntity>(builder =>
        {
            builder.ToTable("templates");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedOnAdd();
            builder.HasIndex(e => new { e.TemplateId, e.Language }).IsUnique();
            builder.Property(e => e.TemplateId).IsRequired().HasMaxLength(255);
            builder.Property(e => e.Language).IsRequired().HasMaxLength(10).HasDefaultValue("en");
            builder.Property(e => e.Name).IsRequired().HasMaxLength(255);
            builder.Property(e => e.Content).IsRequired();
            builder.Property(e => e.Category).IsRequired().HasMaxLength(50);
            builder.Property(e => e.IsDeleted).HasDefaultValue(false);
            builder.Property(e => e.ParamSchema).HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null));
        });
    }
}

#endregion

#region Arbitraries

public record SeedRepetitionInput(int Repetitions)
{
    public override string ToString() => $"Repetitions={Repetitions}";
}

public class SeedRepetitionArbitraries
{
    public static Arbitrary<SeedRepetitionInput> SeedRepetitionInputArbitrary()
    {
        Gen<int> repetitionsGen = FsCheck.Fluent.Gen.Choose(2, 10);
        Gen<SeedRepetitionInput> gen = FsCheck.Fluent.Gen.Select(repetitionsGen, r => new SeedRepetitionInput(r));
        return FsCheck.Fluent.Arb.From(gen);
    }
}

#endregion
