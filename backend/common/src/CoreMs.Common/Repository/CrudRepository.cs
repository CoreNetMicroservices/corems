using Microsoft.EntityFrameworkCore;

namespace CoreMs.Common.Repository;

/// <summary>
/// Base repository providing standard CRUD operations for any entity.
/// Add/Update/Remove track changes in memory; call <see cref="SaveChangesAsync"/>
/// to persist them.
/// </summary>
public abstract class CrudRepository<TEntity>(DbContext context) where TEntity : class
{
    protected readonly DbContext Context = context;
    protected DbSet<TEntity> DbSet => Context.Set<TEntity>();

    public virtual async Task<TEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => await DbSet.FindAsync([id], ct);

    public virtual void Add(TEntity entity) => DbSet.Add(entity);

    public virtual void Update(TEntity entity) => DbSet.Update(entity);

    public virtual void Remove(TEntity entity) => DbSet.Remove(entity);

    /// <summary>
    /// Persists all tracked changes on the underlying context to the database.
    /// Call after Add/Update/Remove or after mutating a tracked entity.
    /// </summary>
    public virtual async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await Context.SaveChangesAsync(ct);
}
