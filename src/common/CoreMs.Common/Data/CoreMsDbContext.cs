using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace CoreMs.Common.Data;

/// <summary>
/// Base DbContext for all CoreMS services. Handles schema configuration
/// and entity discovery from the calling assembly's configurations.
/// </summary>
public abstract class CoreMsDbContext : DbContext
{
    protected abstract string SchemaName { get; }

    protected CoreMsDbContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}
