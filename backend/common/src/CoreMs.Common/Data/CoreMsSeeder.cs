using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreMs.Common.Data;

/// <summary>
/// A database seeder. Implementations populate development/staging data and MUST be idempotent
/// (safe to run repeatedly). Seeders are auto-discovered and run by
/// <c>RunCoreMsDatabaseAsync</c> — register one via <c>builder.Services.AddScoped&lt;ICoreMsSeeder, MySeeder&gt;()</c>
/// or the <c>AddCoreMsSeeder&lt;T&gt;()</c> helper.
/// </summary>
public interface ICoreMsSeeder
{
    /// <summary>Populates seed data. Must be idempotent (safe to run repeatedly).</summary>
    Task SeedAsync(CancellationToken ct = default);

    /// <summary>
    /// Removes this seeder's data so it can be re-seeded from a clean baseline. Destructive —
    /// only invoked by the <c>--reseed</c> path, which is Development-only.
    /// </summary>
    Task ClearAsync(CancellationToken ct = default);
}

/// <summary>
/// Base seeder that encapsulates the common idempotency + logging pattern for a single entity type:
/// skip if data already exists, otherwise insert and save. Implement <see cref="AlreadySeededAsync"/>
/// with a cheap existence check and <see cref="BuildSeedData"/> with the rows to insert.
/// </summary>
/// <typeparam name="TEntity">The entity type this seeder populates.</typeparam>
public abstract class CoreMsSeeder<TEntity> : ICoreMsSeeder
    where TEntity : class
{
    protected DbContext Context { get; }
    protected ILogger Logger { get; }

    protected CoreMsSeeder(DbContext context, ILogger logger)
    {
        Context = context;
        Logger = logger;
    }

    /// <summary>Cheap check for whether seed data already exists (e.g. a marker row).</summary>
    protected abstract Task<bool> AlreadySeededAsync(CancellationToken ct);

    /// <summary>The rows to insert when the store is empty.</summary>
    protected abstract IEnumerable<TEntity> BuildSeedData();

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var name = GetType().Name;

        if (await AlreadySeededAsync(ct))
        {
            Logger.LogInformation("{Seeder}: data already exists — skipping", name);
            return;
        }

        Logger.LogInformation("{Seeder}: seeding...", name);
        Context.Set<TEntity>().AddRange(BuildSeedData());
        var count = await Context.SaveChangesAsync(ct);
        Logger.LogInformation("{Seeder}: complete — {Count} rows written", name, count);
    }

    /// <summary>
    /// Truncates this seeder's table, resetting identity sequences and cascading to FK-dependent
    /// tables (e.g. clearing users also clears their roles). Table and schema are read from the
    /// EF Core model — not hardcoded — and quoted, so there is no injection surface.
    ///
    /// Override to control ordering when a seeder spans multiple independent tables.
    /// </summary>
    public virtual async Task ClearAsync(CancellationToken ct = default)
    {
        var entityType = Context.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"No EF model mapping found for {typeof(TEntity).Name}.");

        var table = entityType.GetTableName()
            ?? throw new InvalidOperationException($"No table name mapped for {typeof(TEntity).Name}.");
        var schema = entityType.GetSchema();

        // Schema-per-service means schema is normally set; guard for the unschematized case.
        var qualified = schema is null ? $"\"{table}\"" : $"\"{schema}\".\"{table}\"";
        var sql = $"TRUNCATE TABLE {qualified} RESTART IDENTITY CASCADE";

        Logger.LogWarning("{Seeder}: truncating {Table} (RESTART IDENTITY CASCADE)", GetType().Name, qualified);

        // EF1002: identifiers cannot be parameterized. The table/schema come from EF model
        // metadata (not user input) and are quoted, so there is no injection surface.
#pragma warning disable EF1002
        await Context.Database.ExecuteSqlRawAsync(sql, ct);
#pragma warning restore EF1002
    }
}
