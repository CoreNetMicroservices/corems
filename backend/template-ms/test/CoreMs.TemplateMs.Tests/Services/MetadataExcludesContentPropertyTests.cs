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
/// Property 10: Metadata response excludes content
/// For any template, the metadata endpoint response should include name, description, category,
/// paramSchema, and required parameters, but should never include the raw template content field.
///
/// **Validates: Requirements 10.1**
/// </summary>
public class MetadataExcludesContentPropertyTests
{
    #region Property 10: Metadata response excludes content

    [Property(MaxTest = 50, Arbitrary = [typeof(MetadataArbitraries)])]
    public async Task GetMetadataAsync_NeverIncludesContent(MetadataInput input)
    {
        var repository = Substitute.For<TemplateRepository>(Substitute.For<DbContext>());
        var engine = new TemplateEngine();
        var cache = new TemplateCache();
        var currentUser = Substitute.For<ICurrentUserService>();

        var entity = new TemplateEntity
        {
            TemplateId = input.TemplateId,
            Language = input.Language,
            Name = input.Name,
            Description = input.Description,
            Content = input.Content,
            Category = input.Category,
            ParamSchema = input.ParamSchema
        };

        repository.GetByTemplateIdAndLanguageAsync(input.TemplateId, input.Language, Arg.Any<CancellationToken>())
            .Returns(entity);

        var service = new TemplateService(repository, engine, cache, currentUser);

        var metadata = await service.GetMetadataAsync(input.TemplateId, input.Language);

        // Metadata includes expected fields
        Assert.Equal(input.TemplateId, metadata.TemplateId);
        Assert.Equal(input.Language, metadata.Language);
        Assert.Equal(input.Name, metadata.Name);
        Assert.Equal(input.Description, metadata.Description);
        Assert.Equal(input.Category, metadata.Category);
        Assert.Equal(input.ParamSchema, metadata.ParamSchema);
        Assert.NotNull(metadata.RequiredParameters);

        // TemplateMetadataDto must NOT have a Content property (verified via reflection)
        var properties = typeof(TemplateMetadataDto).GetProperties();
        Assert.DoesNotContain(properties, p => p.Name == "Content");

        // Additionally verify that the metadata object itself has no way to expose raw content
        var allPropertyValues = properties.Select(p => p.GetValue(metadata)?.ToString() ?? "");
        Assert.DoesNotContain(allPropertyValues, v => v == input.Content);
    }

    [Fact]
    public void TemplateMetadataDto_HasExpectedFieldsButNoContent()
    {
        var properties = typeof(TemplateMetadataDto).GetProperties();
        var propertyNames = properties.Select(p => p.Name).ToList();

        // Must include these fields
        Assert.Contains("TemplateId", propertyNames);
        Assert.Contains("Language", propertyNames);
        Assert.Contains("Name", propertyNames);
        Assert.Contains("Description", propertyNames);
        Assert.Contains("Category", propertyNames);
        Assert.Contains("ParamSchema", propertyNames);
        Assert.Contains("RequiredParameters", propertyNames);

        // Must NOT include Content
        Assert.DoesNotContain("Content", propertyNames);
    }

    #endregion
}

#region Arbitraries

public record MetadataInput(
    string TemplateId,
    string Language,
    string Name,
    string? Description,
    string Content,
    string Category,
    Dictionary<string, object>? ParamSchema)
{
    public override string ToString() => $"{TemplateId}:{Language}";
}

public class MetadataArbitraries
{
    private static readonly string[] TemplateIds = ["welcome-email", "invoice", "notification", "alert", "promo"];
    private static readonly string[] Languages = ["en", "fr", "de", "es", "it"];
    private static readonly string[] Names = ["Welcome", "Invoice", "Notification", "Alert", "Promotion"];
    private static readonly string[] Categories = ["COMMON", "EMAIL", "SMS", "DOCUMENT"];
    private static readonly string[] Descriptions = ["A welcome template", "Invoice template", null!, "Alert notice", "Promotion email"];

    public static Arbitrary<MetadataInput> MetadataInputArbitrary()
    {
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(0, int.MaxValue);
        Gen<MetadataInput> gen = FsCheck.Fluent.Gen.Select(seedGen, seed =>
        {
            var rng = new Random(seed);

            var templateId = TemplateIds[rng.Next(TemplateIds.Length)];
            var language = Languages[rng.Next(Languages.Length)];
            var name = Names[rng.Next(Names.Length)];
            var category = Categories[rng.Next(Categories.Length)];
            var descIdx = rng.Next(Descriptions.Length);
            var description = Descriptions[descIdx];

            // Generate content with random parameters
            var paramCount = rng.Next(1, 4);
            var paramNames = Enumerable.Range(0, paramCount).Select(i => $"param{i}").ToList();
            var content = "SECRET: " + string.Join(" ", paramNames.Select(p => "{{" + p + "}}"));

            var paramSchema = paramNames.ToDictionary(
                p => p,
                p => (object)new Dictionary<string, object> { ["type"] = "string", ["required"] = true });

            return new MetadataInput(templateId, language, name, description, content, category, paramSchema);
        });
        return FsCheck.Fluent.Arb.From(gen);
    }
}

#endregion
