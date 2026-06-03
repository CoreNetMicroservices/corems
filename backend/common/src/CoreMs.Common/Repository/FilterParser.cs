namespace CoreMs.Common.Repository;

/// <summary>
/// Parses filter strings like "field:operation:value" into FilterRequest objects.
/// Only allows fields in the allowedFields set. Resolves aliases.
/// </summary>
public static class FilterParser
{
    public static List<FilterRequest> Parse(
        List<string>? rawFilters,
        IReadOnlySet<string> allowedFields,
        IReadOnlyDictionary<string, string>? aliases = null)
    {
        if (rawFilters == null || rawFilters.Count == 0)
            return [];

        var result = new List<FilterRequest>();
        foreach (var raw in rawFilters)
        {
            var parts = raw.Split(':', 3);
            if (parts.Length < 3) continue;

            var field = aliases != null && aliases.TryGetValue(parts[0], out var mapped) ? mapped : parts[0];
            if (!allowedFields.Contains(field)) continue;

            var op = ParseOperation(parts[1]);
            if (op == null) continue;

            result.Add(new FilterRequest(field, op.Value, parts[2]));
        }
        return result;
    }

    private static FilterOperation? ParseOperation(string op) => op.ToLowerInvariant() switch
    {
        "eq" => FilterOperation.Equals,
        "ne" => FilterOperation.NotEquals,
        "like" => FilterOperation.Like,
        "in" => FilterOperation.In,
        "gt" => FilterOperation.GreaterThan,
        "gte" => FilterOperation.GreaterThanOrEqual,
        "lt" => FilterOperation.LessThan,
        "lte" => FilterOperation.LessThanOrEqual,
        _ => null
    };
}
