using CoreMs.Common.Exceptions;
using CoreMs.Common.Security;
using CoreMs.TemplateMs.Core.Entities;
using CoreMs.TemplateMs.Core.Models;
using CoreMs.TemplateMs.Core.Repositories;
using CoreMs.TemplateMs.Core.Services;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace CoreMs.TemplateMs.Tests.Services;

/// <summary>
/// Property 7: Missing parameter detection
/// For any template with known required parameters, if a render request omits one or more,
/// the TemplateService should return an error listing exactly the missing parameter names.
///
/// **Validates: Requirements 5.3**
/// </summary>
public class MissingParameterPropertyTests
{
    #region Property 7: Missing parameter detection

    [Property(MaxTest = 50, Arbitrary = [typeof(MissingParamArbitraries)])]
    public async Task RenderAsync_WithMissingParams_Returns400WithMissingNames(MissingParamInput input)
    {
        var repository = Substitute.For<TemplateRepository>(Substitute.For<DbContext>());
        var engine = new TemplateEngine();
        var cache = new TemplateCache();
        var currentUser = Substitute.For<ICurrentUserService>();

        var entity = new TemplateEntity
        {
            TemplateId = input.TemplateId,
            Language = "en",
            Content = input.Content,
            Name = "Test",
            Category = "COMMON",
            ParamSchema = input.AllParams.ToDictionary(
                p => p,
                p => (object)new Dictionary<string, object> { ["type"] = "string", ["required"] = true })
        };

        repository.GetByTemplateIdAndLanguageAsync(input.TemplateId, "en", Arg.Any<CancellationToken>())
            .Returns(entity);

        var service = new TemplateService(repository, engine, cache, currentUser);

        var request = new RenderTemplateRequest
        {
            TemplateId = input.TemplateId,
            Language = "en",
            Parameters = input.ProvidedParams
        };

        var ex = await Assert.ThrowsAsync<ServiceException>(() => service.RenderAsync(request));

        Assert.Equal(400, ex.HttpStatusCode);

        var details = ex.Errors[0].Details!;
        foreach (var missing in input.MissingParams)
        {
            Assert.Contains(missing, details);
        }
    }

    [Property(MaxTest = 30, Arbitrary = [typeof(MissingParamArbitraries)])]
    public async Task RenderAsync_WithAllParams_DoesNotThrowMissingParamError(MissingParamInput input)
    {
        var repository = Substitute.For<TemplateRepository>(Substitute.For<DbContext>());
        var engine = new TemplateEngine();
        var cache = new TemplateCache();
        var currentUser = Substitute.For<ICurrentUserService>();

        var entity = new TemplateEntity
        {
            TemplateId = input.TemplateId,
            Language = "en",
            Content = input.Content,
            Name = "Test",
            Category = "COMMON",
            ParamSchema = input.AllParams.ToDictionary(
                p => p,
                p => (object)new Dictionary<string, object> { ["type"] = "string", ["required"] = true })
        };

        repository.GetByTemplateIdAndLanguageAsync(input.TemplateId, "en", Arg.Any<CancellationToken>())
            .Returns(entity);

        var service = new TemplateService(repository, engine, cache, currentUser);

        var allParams = input.AllParams.ToDictionary(p => p, p => (object)"value");
        var request = new RenderTemplateRequest
        {
            TemplateId = input.TemplateId,
            Language = "en",
            Parameters = allParams
        };

        var result = await service.RenderAsync(request);
        Assert.NotNull(result.RenderedContent);
    }

    #endregion
}

#region Arbitraries

public record MissingParamInput(
    string TemplateId,
    string Content,
    IReadOnlyList<string> AllParams,
    IReadOnlyList<string> MissingParams,
    Dictionary<string, object> ProvidedParams)
{
    public override string ToString() => $"{TemplateId} missing: [{string.Join(", ", MissingParams)}]";
}

public class MissingParamArbitraries
{
    private static readonly string[] ParamPool = ["name", "email", "company", "title", "city", "amount"];

    public static Arbitrary<MissingParamInput> MissingParamInputArbitrary()
    {
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(1, int.MaxValue);
        Gen<MissingParamInput> gen = FsCheck.Fluent.Gen.Select(seedGen, seed =>
        {
            var rng = new Random(seed);

            var paramCount = rng.Next(2, 5);
            var allParams = ParamPool.OrderBy(_ => rng.Next()).Take(paramCount).ToList();

            var content = "Template: " + string.Join(" ", allParams.Select(p => "{{" + p + "}}"));

            var omitCount = rng.Next(1, allParams.Count);
            var missing = allParams.OrderBy(_ => rng.Next()).Take(omitCount).ToList();
            var provided = allParams.Except(missing)
                .ToDictionary(p => p, p => (object)"test_value");

            var templateId = $"template-{seed % 1000}";
            return new MissingParamInput(templateId, content, allParams, missing, provided);
        });
        return FsCheck.Fluent.Arb.From(gen);
    }
}

#endregion
