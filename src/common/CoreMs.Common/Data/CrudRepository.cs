using CoreMs.Common.Query;
using Microsoft.EntityFrameworkCore;

namespace CoreMs.Common.Data;

/// <summary>
/// Base repository providing standard CRUD operations for any entity.
/// Methods track changes in memory — actual DB write happens via auto-save middleware
/// at the end of the HTTP request.
/// </summary>
public abstract class CrudRepository<TEntity>(DbContext context) : ICrudRepository<TEntity> where TEntity : class
{
    protected readonly DbContext Context = context;
    protected DbSet<TEntity> DbSet => Context.Set<TEntity>();

    public virtual async Task<TEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => await DbSet.FindAsync([id], ct);

    public virtual void Add(TEntity entity) => DbSet.Add(entity);

    public virtual void Update(TEntity entity) => DbSet.Update(entity);

    public virtual void Remove(TEntity entity) => DbSet.Remove(entity);
}
