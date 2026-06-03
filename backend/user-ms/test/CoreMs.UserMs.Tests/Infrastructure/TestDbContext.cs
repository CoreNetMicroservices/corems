using CoreMs.UserMs.Infrastructure.Data;
using CoreMs.UserMs.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CoreMs.UserMs.Tests.Infrastructure;

/// <summary>
/// Test DbContext that applies the same entity configurations as UserMsDbContext
/// but works with SQLite for constraint enforcement in tests.
/// </summary>
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply the same configurations from the Infrastructure assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserMsDbContext).Assembly);
    }
}
