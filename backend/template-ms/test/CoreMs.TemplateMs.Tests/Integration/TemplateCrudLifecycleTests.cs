using CoreMs.Common.Data;
using CoreMs.Common.Exceptions;
using CoreMs.Common.Security;
using CoreMs.TemplateMs.Core.Entities;
using CoreMs.TemplateMs.Core.Models;
using CoreMs.TemplateMs.Core.Repositories;
using CoreMs.TemplateMs.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NSubstitute;
using Xunit;

namespace CoreMs.TemplateMs.Tests.Integration;

/// <summary>
/// Integration tests for the full template CRUD lifecycle using real TemplateService
/// with SQLite in-memory database, real TemplateEngine, and real TemplateCache.
/// Tests Requirements 1.1, 2.1, 3.1, 5.1, 9.1, 9.2
/// </summary>
public class TemplateCrudLifecycleTests : IDisposable
{
    private readonly DbContext _context;
    private readonly TemplateService _service;
    private readonly TemplateCache _cache;

    public TemplateCrudLifecycleTests()
    {
        var options = new DbContextOptionsBuilder<TestTemplateDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        var dbContext = new TestTemplateDbContext(options);
        dbContext.Database.OpenConnection();
        dbContext.Database.EnsureCreated();
        _context = dbContext;

        var repository = new TemplateRepository(_context);
        var engine = new TemplateEngine();
        _cache = new TemplateCache();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetCurrentUserUuid().Returns(Guid.NewGuid());

        _service = new TemplateService(repository, engine, _cache, currentUser);
    }

    [Fact]
    public async Task FullLifecycle_Create_Read_Update_Delete()
    {
        // CREATE
        var createRequest = new CreateTemplateRequest
        {
            TemplateId = "lifecycle-test",
            Language = "en",
            Name = "Lifecycle Test",
            Content = "Hello {{name}}",
            Category = "COMMON"
        };

        var created = await _service.CreateAsync(createRequest);
        await _context.SaveChangesAsync();

        Assert.Equal("lifecycle-test", created.TemplateId);
        Assert.Equal("en", created.Language);
        Assert.NotEqual(Guid.Empty, created.Id);

        // READ
        var retrieved = await _service.GetByUuidAsync(created.Id);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal("Hello {{name}}", retrieved.Content);

        // UPDATE
        var updateRequest = new UpdateTemplateRequest
        {
            Name = "Updated Lifecycle Test",
            Content = "Hi {{name}}, welcome to {{company}}"
        };

        var updated = await _service.UpdateAsync(created.Id, updateRequest);
        await _context.SaveChangesAsync();

        Assert.Equal("Updated Lifecycle Test", updated.Name);
        Assert.Contains("company", updated.Content);

        // DELETE (soft)
        await _service.DeleteAsync(created.Id);
        await _context.SaveChangesAsync();

        // After soft delete, should throw 404
        var ex = await Assert.ThrowsAsync<ServiceException>(() => _service.GetByUuidAsync(created.Id));
        Assert.Equal(404, ex.HttpStatusCode);
    }

    [Fact]
    public async Task Render_WithValidTemplate_ProducesExpectedOutput()
    {
        var createRequest = new CreateTemplateRequest
        {
            TemplateId = "render-test",
            Language = "en",
            Name = "Render Test",
            Content = "Hello {{name}}, you have {{count}} messages.",
            Category = "COMMON"
        };

        await _service.CreateAsync(createRequest);
        await _context.SaveChangesAsync();

        var renderRequest = new RenderTemplateRequest
        {
            TemplateId = "render-test",
            Language = "en",
            Parameters = new Dictionary<string, object>
            {
                ["name"] = "Alice",
                ["count"] = "5"
            }
        };

        var result = await _service.RenderAsync(renderRequest);

        Assert.Equal("Hello Alice, you have 5 messages.", result.RenderedContent);
    }

    [Fact]
    public async Task Render_SoftDeletedTemplate_Returns404()
    {
        var createRequest = new CreateTemplateRequest
        {
            TemplateId = "deletable-template",
            Language = "en",
            Name = "Deletable",
            Content = "Hello {{name}}",
            Category = "COMMON"
        };

        var created = await _service.CreateAsync(createRequest);
        await _context.SaveChangesAsync();

        await _service.DeleteAsync(created.Id);
        await _context.SaveChangesAsync();

        var renderRequest = new RenderTemplateRequest
        {
            TemplateId = "deletable-template",
            Language = "en",
            Parameters = new Dictionary<string, object> { ["name"] = "test" }
        };

        var ex = await Assert.ThrowsAsync<ServiceException>(() => _service.RenderAsync(renderRequest));
        Assert.Equal(404, ex.HttpStatusCode);
    }

    [Fact]
    public async Task Create_DuplicateTemplateIdAndLanguage_Returns409()
    {
        var request = new CreateTemplateRequest
        {
            TemplateId = "duplicate-test",
            Language = "en",
            Name = "First",
            Content = "Hello {{name}}",
            Category = "COMMON"
        };

        await _service.CreateAsync(request);
        await _context.SaveChangesAsync();

        var duplicateRequest = new CreateTemplateRequest
        {
            TemplateId = "duplicate-test",
            Language = "en",
            Name = "Second (duplicate)",
            Content = "Different content {{x}}",
            Category = "EMAIL"
        };

        var ex = await Assert.ThrowsAsync<ServiceException>(() => _service.CreateAsync(duplicateRequest));
        Assert.Equal(409, ex.HttpStatusCode);
    }

    [Fact]
    public async Task Update_CacheInvalidation_RenderUsesNewContent()
    {
        var createRequest = new CreateTemplateRequest
        {
            TemplateId = "cache-test",
            Language = "en",
            Name = "Cache Test",
            Content = "Version 1: {{name}}",
            Category = "COMMON"
        };

        var created = await _service.CreateAsync(createRequest);
        await _context.SaveChangesAsync();

        // Render once to populate cache
        var renderRequest = new RenderTemplateRequest
        {
            TemplateId = "cache-test",
            Language = "en",
            Parameters = new Dictionary<string, object> { ["name"] = "Alice" }
        };

        var result1 = await _service.RenderAsync(renderRequest);
        Assert.Contains("Version 1", result1.RenderedContent);

        // Update content
        var updateRequest = new UpdateTemplateRequest { Content = "Version 2: {{name}}" };
        await _service.UpdateAsync(created.Id, updateRequest);
        await _context.SaveChangesAsync();

        // Render again — should get Version 2 (cache invalidated)
        var result2 = await _service.RenderAsync(renderRequest);
        Assert.Contains("Version 2", result2.RenderedContent);
    }

    [Fact]
    public async Task GetMetadata_ReturnsMetadataWithoutContent()
    {
        var createRequest = new CreateTemplateRequest
        {
            TemplateId = "metadata-test",
            Language = "en",
            Name = "Metadata Test",
            Content = "Hello {{name}} from {{company}}",
            Category = "EMAIL"
        };

        await _service.CreateAsync(createRequest);
        await _context.SaveChangesAsync();

        var metadata = await _service.GetMetadataAsync("metadata-test", "en");

        Assert.Equal("metadata-test", metadata.TemplateId);
        Assert.Equal("en", metadata.Language);
        Assert.Equal("Metadata Test", metadata.Name);
        Assert.Equal("EMAIL", metadata.Category);
        Assert.NotNull(metadata.ParamSchema);
        Assert.Contains("name", metadata.RequiredParameters);
        Assert.Contains("company", metadata.RequiredParameters);
    }

    [Fact]
    public async Task Delete_NonExistentTemplate_Returns404()
    {
        var ex = await Assert.ThrowsAsync<ServiceException>(
            () => _service.DeleteAsync(Guid.NewGuid()));
        Assert.Equal(404, ex.HttpStatusCode);
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }
}

/// <summary>
/// Test-specific DbContext that uses SQLite-compatible entity configuration.
/// Avoids PostgreSQL-specific features (UseIdentityAlwaysColumn, jsonb, CURRENT_TIMESTAMP).
/// </summary>
file class TestTemplateDbContext : CoreMsDbContext
{
    protected override string SchemaName => "template_ms";

    public TestTemplateDbContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // SQLite does not support schemas — skip default schema
        modelBuilder.ApplyConfiguration(new SqliteTemplateEntityConfiguration());
    }
}

file class SqliteTemplateEntityConfiguration : IEntityTypeConfiguration<TemplateEntity>
{
    public void Configure(EntityTypeBuilder<TemplateEntity> builder)
    {
        builder.ToTable("templates");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.HasIndex(e => e.Uuid).IsUnique();
        builder.HasIndex(e => new { e.TemplateId, e.Language }).IsUnique();

        builder.Property(e => e.Uuid).IsRequired();
        builder.Property(e => e.TemplateId).IsRequired().HasMaxLength(255);
        builder.Property(e => e.Language).IsRequired().HasMaxLength(10).HasDefaultValue("en");
        builder.Property(e => e.Name).IsRequired().HasMaxLength(255);
        builder.Property(e => e.Description);
        builder.Property(e => e.Content).IsRequired();
        builder.Property(e => e.Category).IsRequired().HasMaxLength(50);
        builder.Property(e => e.ParamSchema).HasConversion(
            v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
            v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null));
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();
        builder.Property(e => e.CreatedBy);
        builder.Property(e => e.UpdatedBy);
        builder.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);
    }
}
