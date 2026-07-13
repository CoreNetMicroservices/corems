namespace CoreMs.Common.Repository;

/// <summary>
/// Represents a paginated result set.
/// </summary>
/// <typeparam name="T">The type of items in the result.</typeparam>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int TotalElements { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalElements / PageSize) : 0;

    public PagedResult(IReadOnlyList<T> items, int totalElements, int page, int pageSize)
    {
        Items = items;
        TotalElements = totalElements;
        Page = page;
        PageSize = pageSize;
    }
}
