namespace CoreMs.Common.Query;

/// <summary>
/// Generic repository interface providing paginated search, sort, and filter capabilities.
/// Each implementing repository defines which fields are searchable and sortable.
/// </summary>
public interface ISearchableRepository<TEntity> : ICrudRepository<TEntity> where TEntity : class
{
    Task<PagedResult<TEntity>> GetPagedAsync(QueryParameters parameters, CancellationToken ct = default);
}
