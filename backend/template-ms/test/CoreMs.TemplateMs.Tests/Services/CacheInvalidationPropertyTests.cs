using CoreMs.TemplateMs.Core.Services;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace CoreMs.TemplateMs.Tests.Services;

/// <summary>
/// Property 3: Cache invalidation correctness
/// For any template that is updated or deleted, subsequent render requests should use the new content
/// (not stale cached content). Specifically: after Invalidate(), Get() should return null.
///
/// **Validates: Requirements 2.4, 3.2, 6.3**
/// </summary>
public class CacheInvalidationPropertyTests
{
    #region Property 3: Cache invalidation correctness

    [Property(MaxTest = 50, Arbitrary = [typeof(CacheArbitraries)])]
    public void Cache_AfterInvalidate_GetReturnsNull(CacheKeyInput input)
    {
        var cache = new TemplateCache();
        var engine = new TemplateEngine();
        var compiled = engine.Compile("Hello {{name}}");

        cache.Set(input.TemplateId, input.Language, compiled);

        Assert.NotNull(cache.Get(input.TemplateId, input.Language));

        cache.Invalidate(input.TemplateId, input.Language);

        Assert.Null(cache.Get(input.TemplateId, input.Language));
    }

    [Property(MaxTest = 50, Arbitrary = [typeof(CacheArbitraries)])]
    public void Cache_AfterSet_GetReturnsSameTemplate(CacheKeyInput input)
    {
        var cache = new TemplateCache();
        var engine = new TemplateEngine();
        var compiled = engine.Compile("Hello {{name}}");

        cache.Set(input.TemplateId, input.Language, compiled);

        var retrieved = cache.Get(input.TemplateId, input.Language);
        Assert.NotNull(retrieved);
        Assert.Same(compiled, retrieved);
    }

    [Property(MaxTest = 50, Arbitrary = [typeof(CacheArbitraries)])]
    public void Cache_UpdateOverwritesPrevious(CacheKeyInput input)
    {
        var cache = new TemplateCache();
        var engine = new TemplateEngine();
        var compiled1 = engine.Compile("Version 1: {{name}}");
        var compiled2 = engine.Compile("Version 2: {{name}}");

        cache.Set(input.TemplateId, input.Language, compiled1);
        cache.Set(input.TemplateId, input.Language, compiled2);

        var retrieved = cache.Get(input.TemplateId, input.Language);
        Assert.Same(compiled2, retrieved);
    }

    [Property(MaxTest = 30, Arbitrary = [typeof(CacheArbitraries)])]
    public void Cache_InvalidateOneLanguage_DoesNotAffectOther(CacheKeyInput input)
    {
        var cache = new TemplateCache();
        var engine = new TemplateEngine();
        var compiledEn = engine.Compile("English: {{name}}");
        var compiledFr = engine.Compile("French: {{name}}");

        cache.Set(input.TemplateId, "en", compiledEn);
        cache.Set(input.TemplateId, "fr", compiledFr);

        cache.Invalidate(input.TemplateId, "en");

        Assert.Null(cache.Get(input.TemplateId, "en"));
        Assert.NotNull(cache.Get(input.TemplateId, "fr"));
    }

    #endregion
}

#region Arbitraries

public record CacheKeyInput(string TemplateId, string Language)
{
    public override string ToString() => $"{TemplateId}:{Language}";
}

public class CacheArbitraries
{
    public static Arbitrary<CacheKeyInput> CacheKeyInputArbitrary()
    {
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(0, int.MaxValue);
        Gen<CacheKeyInput> gen = FsCheck.Fluent.Gen.Select(seedGen, seed =>
        {
            var rng = new Random(seed);
            var templateIds = new[] { "welcome-email", "password-reset", "invoice", "notification", "alert" };
            var languages = new[] { "en", "fr", "de", "es", "pt" };
            var templateId = templateIds[rng.Next(templateIds.Length)];
            var language = languages[rng.Next(languages.Length)];
            return new CacheKeyInput(templateId, language);
        });
        return FsCheck.Fluent.Arb.From(gen);
    }
}

#endregion
