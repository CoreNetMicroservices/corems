using System.Linq.Expressions;
using System.Reflection;
using CoreMs.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CoreMs.Common.Repository;

/// <summary>
/// Base repository providing dynamic search, filter, sort, and pagination over any EF Core entity.
/// Subclasses define SearchFields, SortFields, FilterFields, and FieldAliases to control which properties are queryable.
/// Property metadata is cached at construction time — no reflection at query time.
/// </summary>
public abstract class SearchableRepository<TEntity> : CrudRepository<TEntity>
    where TEntity : class
{
    private readonly Dictionary<string, PropertyInfo> _propertyCache;

    // Cached Queryable.OrderBy/OrderByDescending method info
    private static readonly MethodInfo OrderByMethod = typeof(Queryable).GetMethods()
        .First(m => m.Name == "OrderBy" && m.GetParameters().Length == 2);

    private static readonly MethodInfo OrderByDescendingMethod = typeof(Queryable).GetMethods()
        .First(m => m.Name == "OrderByDescending" && m.GetParameters().Length == 2);

    protected SearchableRepository(DbContext context) : base(context)
    {
        _propertyCache = typeof(TEntity)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

        ValidateFields();
    }

    protected abstract IReadOnlySet<string> SearchFields { get; }
    protected abstract IReadOnlySet<string> SortFields { get; }
    protected virtual IReadOnlySet<string> FilterFields => new HashSet<string>();
    protected virtual IReadOnlyDictionary<string, string> FieldAliases => new Dictionary<string, string>();
    protected virtual IQueryable<TEntity> BaseQuery() => DbSet.AsQueryable();

    /// <summary>
    /// Validates that all declared fields exist on the entity. Throws at construction time if not.
    /// </summary>
    private void ValidateFields()
    {
        var entityName = typeof(TEntity).Name;

        var searchFields = SearchFields;
        if (searchFields != null)
        {
            foreach (var field in searchFields)
            {
                if (!_propertyCache.ContainsKey(field))
                    throw new InvalidOperationException($"SearchField '{field}' does not exist on entity '{entityName}'.");
                if (_propertyCache[field].PropertyType != typeof(string))
                    throw new InvalidOperationException($"SearchField '{field}' on entity '{entityName}' must be a string property.");
            }
        }

        var sortFields = SortFields;
        if (sortFields != null)
        {
            foreach (var field in sortFields)
            {
                if (!_propertyCache.ContainsKey(field))
                    throw new InvalidOperationException($"SortField '{field}' does not exist on entity '{entityName}'.");
            }
        }

        var filterFields = FilterFields;
        if (filterFields != null)
        {
            foreach (var field in filterFields)
            {
                if (!_propertyCache.ContainsKey(field))
                    throw new InvalidOperationException($"FilterField '{field}' does not exist on entity '{entityName}'.");
            }
        }
    }

    public virtual async Task<PagedResult<TEntity>> GetPagedAsync(QueryParameters parameters, CancellationToken ct = default)
    {
        var query = BaseQuery();

        if (!string.IsNullOrWhiteSpace(parameters.Search))
            query = ApplySearch(query, parameters.Search);

        if (parameters.Filters is { Count: > 0 })
        {
            var filterRequests = FilterParser.Parse(parameters.Filters, FilterFields, FieldAliases);
            query = ApplyFilters(query, filterRequests);
        }

        query = ApplySort(query, parameters.Sort);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(ct);

        return new PagedResult<TEntity>(items, totalCount, parameters.Page, parameters.PageSize);
    }

    private IQueryable<TEntity> ApplySearch(IQueryable<TEntity> query, string search)
    {
        var searchLower = search.ToLower();
        var parameter = Expression.Parameter(typeof(TEntity), "e");
        Expression? combined = null;

        foreach (var fieldName in SearchFields)
        {
            var prop = _propertyCache[fieldName]; // validated at construction — safe

            var propertyAccess = Expression.Property(parameter, prop);
            var nullCheck = Expression.NotEqual(propertyAccess, Expression.Constant(null, typeof(string)));
            var toLower = Expression.Call(propertyAccess, nameof(string.ToLower), Type.EmptyTypes);
            var contains = Expression.Call(toLower, nameof(string.Contains), null, Expression.Constant(searchLower));
            var expr = Expression.AndAlso(nullCheck, contains);

            combined = combined == null ? expr : Expression.OrElse(combined, expr);
        }

        if (combined == null) return query;
        return query.Where(Expression.Lambda<Func<TEntity, bool>>(combined, parameter));
    }

    private IQueryable<TEntity> ApplyFilters(IQueryable<TEntity> query, List<FilterRequest> filters)
    {
        foreach (var filter in filters)
        {
            if (!_propertyCache.TryGetValue(filter.Field, out var prop)) continue;

            var parameter = Expression.Parameter(typeof(TEntity), "e");
            var propertyAccess = Expression.Property(parameter, prop);
            var filterExpr = BuildFilterExpression(propertyAccess, prop.PropertyType, filter.Operation, filter.Value, filter.Field);
            if (filterExpr == null) continue;

            var lambda = Expression.Lambda<Func<TEntity, bool>>(filterExpr, parameter);
            query = query.Where(lambda);
        }

        return query;
    }

    private static Expression? BuildFilterExpression(Expression property, Type propertyType, FilterOperation op, string rawValue, string fieldName)
    {
        if (op == FilterOperation.Like)
            return BuildLikeExpression(property, propertyType, rawValue);

        if (op == FilterOperation.In)
            return BuildInExpression(property, rawValue, propertyType, fieldName);

        var value = ConvertValueSafe(rawValue, propertyType, fieldName);
        if (value == null && op != FilterOperation.Equals && op != FilterOperation.NotEquals) return null;

        var constant = Expression.Constant(value, propertyType);

        return op switch
        {
            FilterOperation.Equals => Expression.Equal(property, constant),
            FilterOperation.NotEquals => Expression.NotEqual(property, constant),
            FilterOperation.GreaterThan => Expression.GreaterThan(property, constant),
            FilterOperation.GreaterThanOrEqual => Expression.GreaterThanOrEqual(property, constant),
            FilterOperation.LessThan => Expression.LessThan(property, constant),
            FilterOperation.LessThanOrEqual => Expression.LessThanOrEqual(property, constant),
            _ => null
        };
    }

    private static BinaryExpression? BuildLikeExpression(Expression property, Type propertyType, string value)
    {
        if (propertyType != typeof(string)) return null;

        var nullCheck = Expression.NotEqual(property, Expression.Constant(null, typeof(string)));
        var toLower = Expression.Call(property, nameof(string.ToLower), Type.EmptyTypes);
        var contains = Expression.Call(toLower, nameof(string.Contains), null, Expression.Constant(value.ToLower()));
        return Expression.AndAlso(nullCheck, contains);
    }

    private static MethodCallExpression BuildInExpression(Expression property, string rawValue, Type propertyType, string fieldName)
    {
        var values = rawValue.Split(',').Select(v => ConvertValueSafe(v.Trim(), propertyType, fieldName)).ToList();
        var listType = typeof(List<>).MakeGenericType(propertyType);
        var list = Activator.CreateInstance(listType)!;
        var addMethod = listType.GetMethod("Add")!;
        foreach (var v in values) addMethod.Invoke(list, [v]);

        var containsMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
            .MakeGenericMethod(propertyType);

        return Expression.Call(containsMethod, Expression.Constant(list), property);
    }

    /// <summary>
    /// Converts a string value to the target type. Throws ServiceException with 400 on invalid input.
    /// </summary>
    private static object? ConvertValueSafe(string value, Type targetType, string fieldName)
    {
        try
        {
            return ConvertValue(value, targetType);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or OverflowException)
        {
            var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
            throw ServiceException.Of(
                DefaultErrors.InvalidInput,
                $"Invalid value '{value}' for field '{fieldName}' (expected {underlying.Name})");
        }
    }

    private static object? ConvertValue(string value, Type targetType)
    {
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlying == typeof(string)) return value;
        if (underlying == typeof(bool)) return bool.Parse(value);
        if (underlying == typeof(int)) return int.Parse(value);
        if (underlying == typeof(long)) return long.Parse(value);
        if (underlying == typeof(double)) return double.Parse(value);
        if (underlying == typeof(decimal)) return decimal.Parse(value);
        if (underlying == typeof(Guid)) return Guid.Parse(value);
        if (underlying == typeof(DateTime)) return DateTime.Parse(value).ToUniversalTime();
        if (underlying == typeof(DateTimeOffset)) return DateTimeOffset.Parse(value);
        if (underlying.IsEnum) return Enum.Parse(underlying, value, ignoreCase: true);

        return null;
    }

    private IQueryable<TEntity> ApplySort(IQueryable<TEntity> query, string? sort)
    {
        string fieldName;
        bool descending;

        if (!string.IsNullOrWhiteSpace(sort) && sort.Contains(':'))
        {
            var parts = sort.Split(':');
            var resolvedField = FieldAliases.TryGetValue(parts[0], out var alias) ? alias : parts[0];
            if (SortFields.Contains(resolvedField, StringComparer.OrdinalIgnoreCase))
            {
                fieldName = resolvedField;
                descending = parts.Length > 1 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                fieldName = SortFields.First();
                descending = false;
            }
        }
        else
        {
            fieldName = SortFields.FirstOrDefault() ?? "Id";
            descending = true;
        }

        if (!_propertyCache.TryGetValue(fieldName, out var prop)) return query;

        var parameter = Expression.Parameter(typeof(TEntity), "e");
        var propertyAccess = Expression.Property(parameter, prop);
        var lambda = Expression.Lambda(propertyAccess, parameter);

        var method = (descending ? OrderByDescendingMethod : OrderByMethod)
            .MakeGenericMethod(typeof(TEntity), prop.PropertyType);

        return (IQueryable<TEntity>)method.Invoke(null, [query, lambda])!;
    }
}
