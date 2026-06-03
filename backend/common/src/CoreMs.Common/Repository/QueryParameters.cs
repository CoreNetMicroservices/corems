namespace CoreMs.Common.Repository;

/// <summary>
/// Represents common query parameters for paginated, searchable, filterable, and sortable list endpoints.
/// </summary>
public class QueryParameters
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;

    private int _page = 1;
    private int _pageSize = DefaultPageSize;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }

    /// <summary>
    /// Sort expression. Format: "fieldName:asc" or "fieldName:desc". Default: first allowed sort field descending.
    /// </summary>
    public string? Sort { get; set; }

    /// <summary>
    /// Free-text search across configured search fields.
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Filter expressions. Format: "field:operation:value" (e.g., "isActive:eq:true", "createdAt:gte:2024-01-01")
    /// </summary>
    public List<string>? Filters { get; set; }
}
