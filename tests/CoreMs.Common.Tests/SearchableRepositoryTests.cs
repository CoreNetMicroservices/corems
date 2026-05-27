using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using CoreMs.Common.Query;
using CoreMs.Common.Data;

namespace CoreMs.Common.Tests;

public class TestEntity
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public string? Provider { get; set; }
}

public class TestDbContext : DbContext
{
    public DbSet<TestEntity> TestEntities => Set<TestEntity>();

    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
}

public class TestSearchableRepository : SearchableRepository<TestEntity>
{
    protected override IReadOnlySet<string> SearchFields { get; } =
        new HashSet<string> { "Name", "Email" };

    protected override IReadOnlySet<string> SortFields { get; } =
        new HashSet<string> { "CreatedAt", "Name", "Email" };

    protected override IReadOnlySet<string> FilterFields { get; } =
        new HashSet<string> { "IsActive", "Provider", "CreatedAt" };

    protected override IReadOnlyDictionary<string, string> FieldAliases { get; } =
        new Dictionary<string, string> { ["created"] = "CreatedAt" };

    public TestSearchableRepository(DbContext context) : base(context) { }
}

public class SearchableRepositoryTests : IDisposable
{
    private readonly TestDbContext _context;
    private readonly TestSearchableRepository _repository;

    public SearchableRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new TestDbContext(options);
        _repository = new TestSearchableRepository(_context);
        SeedData();
    }

    private void SeedData()
    {
        _context.TestEntities.AddRange(
            new TestEntity { Id = 1, Name = "Alice Smith", Email = "alice@example.com", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), IsActive = true, Provider = "google" },
            new TestEntity { Id = 2, Name = "Bob Johnson", Email = "bob@example.com", CreatedAt = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc), IsActive = true, Provider = "github" },
            new TestEntity { Id = 3, Name = "Charlie Brown", Email = "charlie@example.com", CreatedAt = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc), IsActive = false, Provider = "google" },
            new TestEntity { Id = 4, Name = "Diana Prince", Email = "john.diana@example.com", CreatedAt = new DateTime(2024, 4, 1, 0, 0, 0, DateTimeKind.Utc), IsActive = true, Provider = "local" },
            new TestEntity { Id = 5, Name = "John Doe", Email = "johndoe@example.com", CreatedAt = new DateTime(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc), IsActive = true, Provider = "google" },
            new TestEntity { Id = 6, Name = null, Email = "nullname@example.com", CreatedAt = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc), IsActive = false, Provider = "local" }
        );
        _context.SaveChanges();
    }

    public void Dispose() => _context.Dispose();

    // --- Search Tests ---

    [Fact]
    public async Task GetPagedAsync_ReturnsAllItems_WhenNoSearchOrFilter()
    {
        var result = await _repository.GetPagedAsync(new QueryParameters { PageSize = 20 });

        result.TotalCount.Should().Be(6);
        result.Items.Should().HaveCount(6);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetPagedAsync_SearchesAcrossConfiguredFields()
    {
        var result = await _repository.GetPagedAsync(new QueryParameters { Search = "john" });

        result.Items.Should().HaveCountGreaterThanOrEqualTo(2);
        result.Items.Should().Contain(e => e.Name == "John Doe");
        result.Items.Should().Contain(e => e.Email == "john.diana@example.com");
    }

    [Fact]
    public async Task GetPagedAsync_SearchIsCaseInsensitive()
    {
        var result = await _repository.GetPagedAsync(new QueryParameters { Search = "JOHN" });

        result.Items.Should().HaveCountGreaterThanOrEqualTo(2);
        result.Items.Should().Contain(e => e.Name == "John Doe");
        result.Items.Should().Contain(e => e.Email == "john.diana@example.com");
    }

    [Fact]
    public async Task GetPagedAsync_HandlesNullableStringFields()
    {
        var result = await _repository.GetPagedAsync(new QueryParameters { Search = "nullname" });

        result.Items.Should().HaveCount(1);
        result.Items.First().Email.Should().Be("nullname@example.com");
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsEmptyResult_WhenSearchMatchesNothing()
    {
        var result = await _repository.GetPagedAsync(new QueryParameters { Search = "zzz" });

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    // --- Sort Tests ---

    [Fact]
    public async Task GetPagedAsync_SortsBySpecifiedField_Ascending()
    {
        var result = await _repository.GetPagedAsync(new QueryParameters
        {
            Sort = "Name:asc",
            PageSize = 20
        });

        var names = result.Items.Select(e => e.Name).ToList();
        names.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetPagedAsync_SortsBySpecifiedField_Descending()
    {
        var result = await _repository.GetPagedAsync(new QueryParameters
        {
            Sort = "Name:desc",
            PageSize = 20
        });

        var names = result.Items.Select(e => e.Name).ToList();
        names.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetPagedAsync_FallsBackToFirstSortField_WhenInvalidSort()
    {
        var resultDefault = await _repository.GetPagedAsync(new QueryParameters
        {
            Sort = "InvalidField:asc",
            PageSize = 20
        });

        var resultExplicit = await _repository.GetPagedAsync(new QueryParameters
        {
            Sort = "CreatedAt:asc",
            PageSize = 20
        });

        resultDefault.Items.Select(e => e.Id).Should().Equal(resultExplicit.Items.Select(e => e.Id));
    }

    [Fact]
    public async Task GetPagedAsync_SortResolvesAliases()
    {
        var resultAlias = await _repository.GetPagedAsync(new QueryParameters
        {
            Sort = "created:asc",
            PageSize = 20
        });

        var resultDirect = await _repository.GetPagedAsync(new QueryParameters
        {
            Sort = "CreatedAt:asc",
            PageSize = 20
        });

        resultAlias.Items.Select(e => e.Id).Should().Equal(resultDirect.Items.Select(e => e.Id));
    }

    // --- Pagination Tests ---

    [Fact]
    public async Task GetPagedAsync_RespectsPageSize()
    {
        var result = await _repository.GetPagedAsync(new QueryParameters { PageSize = 2 });

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(6);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsCorrectPage()
    {
        var page1 = await _repository.GetPagedAsync(new QueryParameters { Page = 1, PageSize = 2 });
        var page2 = await _repository.GetPagedAsync(new QueryParameters { Page = 2, PageSize = 2 });

        page2.Items.Should().HaveCount(2);
        page2.Page.Should().Be(2);
        page2.Items.Should().NotIntersectWith(page1.Items);
    }

    // --- Filter Tests ---

    [Fact]
    public async Task GetPagedAsync_FilterEquals_Boolean()
    {
        var result = await _repository.GetPagedAsync(new QueryParameters
        {
            Filters = ["IsActive:eq:true"],
            PageSize = 20
        });

        result.Items.Should().OnlyContain(e => e.IsActive);
        result.TotalCount.Should().Be(4);
    }

    [Fact]
    public async Task GetPagedAsync_FilterNotEquals()
    {
        var result = await _repository.GetPagedAsync(new QueryParameters
        {
            Filters = ["Provider:ne:google"],
            PageSize = 20
        });

        result.Items.Should().OnlyContain(e => e.Provider != "google");
    }

    [Fact]
    public async Task GetPagedAsync_FilterLike()
    {
        var result = await _repository.GetPagedAsync(new QueryParameters
        {
            Filters = ["Provider:like:goo"],
            PageSize = 20
        });

        result.Items.Should().OnlyContain(e => e.Provider != null && e.Provider.Contains("goo"));
    }

    [Fact]
    public async Task GetPagedAsync_FilterIn()
    {
        var result = await _repository.GetPagedAsync(new QueryParameters
        {
            Filters = ["Provider:in:google,github"],
            PageSize = 20
        });

        result.Items.Should().OnlyContain(e => e.Provider == "google" || e.Provider == "github");
        result.TotalCount.Should().Be(4);
    }

    [Fact]
    public async Task GetPagedAsync_FilterGreaterThanOrEqual_DateTime()
    {
        var result = await _repository.GetPagedAsync(new QueryParameters
        {
            Filters = ["CreatedAt:gte:2024-04-01"],
            PageSize = 20
        });

        result.Items.Should().OnlyContain(e => e.CreatedAt >= new DateTime(2024, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetPagedAsync_FilterLessThan_DateTime()
    {
        var result = await _repository.GetPagedAsync(new QueryParameters
        {
            Filters = ["CreatedAt:lt:2024-03-01"],
            PageSize = 20
        });

        result.Items.Should().OnlyContain(e => e.CreatedAt < new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetPagedAsync_MultipleFilters_CombinedWithAnd()
    {
        var result = await _repository.GetPagedAsync(new QueryParameters
        {
            Filters = ["IsActive:eq:true", "Provider:eq:google"],
            PageSize = 20
        });

        result.Items.Should().OnlyContain(e => e.IsActive && e.Provider == "google");
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetPagedAsync_FilterIgnoresDisallowedFields()
    {
        var result = await _repository.GetPagedAsync(new QueryParameters
        {
            Filters = ["Name:eq:Alice Smith"],
            PageSize = 20
        });

        // Name is not in FilterFields, so filter is ignored — all items returned
        result.TotalCount.Should().Be(6);
    }

    [Fact]
    public async Task GetPagedAsync_SearchAndFilterCombined()
    {
        var result = await _repository.GetPagedAsync(new QueryParameters
        {
            Search = "john",
            Filters = ["IsActive:eq:true"],
            PageSize = 20
        });

        result.Items.Should().OnlyContain(e => e.IsActive);
        result.Items.Should().HaveCountGreaterThanOrEqualTo(2);
    }
}
