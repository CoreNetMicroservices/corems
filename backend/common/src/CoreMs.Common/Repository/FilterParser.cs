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
            if (parts.Length < 2) continue;

            string field;
            FilterOperation op;
            string value;

            if (parts.Length == 2)
            {
                // "field:value" — default to eq
                field = parts[0];
                op = FilterOperation.Equals;
                value = parts[1];
            }
            else
            {
                // "field:operation:value"
                field = parts[0];
                var parsed = ParseOperation(parts[1]);
                if (parsed == null) continue;
                op = parsed.Value;
                value = parts[2];
            }

            field = aliases != null && aliases.TryGetValue(field, out var mapped) ? mapped : field;

            // Case-insensitive match against allowed fields
            var matchedField = allowedFields.FirstOrDefault(f => f.Equals(field, StringComparison.OrdinalIgnoreCase));
            if (matchedField == null) continue;

            result.Add(new FilterRequest(matchedField, op, value));
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
