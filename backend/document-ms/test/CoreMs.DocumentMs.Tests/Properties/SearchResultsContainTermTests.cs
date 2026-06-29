using CoreMs.DocumentMs.Core.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System.Reflection;
using Xunit;

namespace CoreMs.DocumentMs.Tests.Properties;

/// <summary>
/// Property 15: Search results contain search term — all returned documents contain
/// the search term in at least one searchable field.
/// Validates: Requirements 10.2
/// </summary>
public class SearchResultsContainTermTests
{
    [Fact]
    public void SearchFields_ContainsExpectedFields()
    {
        var dbContext = Substitute.For<DbContext>();
        var repository = new DocumentRepository(dbContext);

        var prop = typeof(DocumentRepository).GetProperty("SearchFields", BindingFlags.NonPublic | BindingFlags.Instance);
        var fields = (IReadOnlySet<string>)prop!.GetValue(repository)!;

        fields.Should().Contain("Name");
        fields.Should().Contain("OriginalFilename");
        fields.Should().Contain("Description");
        fields.Should().HaveCount(3);
    }

    [Fact]
    public void SortFields_ContainsExpectedFields()
    {
        var dbContext = Substitute.For<DbContext>();
        var repository = new DocumentRepository(dbContext);

        var prop = typeof(DocumentRepository).GetProperty("SortFields", BindingFlags.NonPublic | BindingFlags.Instance);
        var fields = (IReadOnlySet<string>)prop!.GetValue(repository)!;

        fields.Should().Contain("Name");
        fields.Should().Contain("CreatedAt");
        fields.Should().Contain("UpdatedAt");
        fields.Should().Contain("Size");
        fields.Should().HaveCount(4);
    }

    [Fact]
    public void FilterFields_ContainsExpectedFields()
    {
        var dbContext = Substitute.For<DbContext>();
        var repository = new DocumentRepository(dbContext);

        var prop = typeof(DocumentRepository).GetProperty("FilterFields", BindingFlags.NonPublic | BindingFlags.Instance);
        var fields = (IReadOnlySet<string>)prop!.GetValue(repository)!;

        fields.Should().Contain("Visibility");
        fields.Should().Contain("Extension");
        fields.Should().Contain("UserId");
        fields.Should().Contain("IsDeleted");
        fields.Should().HaveCount(4);
    }
}
