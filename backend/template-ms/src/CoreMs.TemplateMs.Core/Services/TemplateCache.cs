using System.Collections.Concurrent;
using CoreMs.Common.Extensions;
using HandlebarsDotNet;
using Microsoft.Extensions.DependencyInjection;

namespace CoreMs.TemplateMs.Core.Services;

[Service(ServiceLifetime.Singleton)]
public class TemplateCache
{
    private readonly ConcurrentDictionary<string, HandlebarsTemplate<object, object>> _cache = new();

    public HandlebarsTemplate<object, object>? Get(string templateId, string language)
    {
        var key = BuildKey(templateId, language);
        return _cache.TryGetValue(key, out var compiled) ? compiled : null;
    }

    public void Set(string templateId, string language, HandlebarsTemplate<object, object> compiled)
    {
        var key = BuildKey(templateId, language);
        _cache[key] = compiled;
    }

    public void Invalidate(string templateId, string language)
    {
        var key = BuildKey(templateId, language);
        _cache.TryRemove(key, out _);
    }

    private static string BuildKey(string templateId, string language) => $"{templateId}:{language}";
}
