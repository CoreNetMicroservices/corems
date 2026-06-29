using CoreMs.DocumentMs.Core.Services;
using FsCheck;
using FsCheck.Xunit;
using FluentAssertions;
using Xunit;

namespace CoreMs.DocumentMs.Tests.Properties;

/// <summary>
/// **Validates: Requirements 14.1, 14.2, 14.3**
/// Property 8: File extension extraction — For any valid filename, extract substring
/// after last dot (lowercased) or empty string if no dot.
/// </summary>
public class FileExtensionExtractionTests
{
    [Property(MaxTest = 50, Arbitrary = [typeof(ExtensionExtractionArbitraries)])]
    public void ExtractedExtension_IsAlwaysLowercase(MixedCaseFilename input)
    {
        var ext = DocumentService.ExtractExtension(input.Value);
        ext.Should().Be(ext.ToLowerInvariant());
    }

    [Property(MaxTest = 50, Arbitrary = [typeof(ExtensionExtractionArbitraries)])]
    public void FilenameWithDot_ReturnsSubstringAfterLastDot(SimpleFilenameWithExtension input)
    {
        var result = DocumentService.ExtractExtension(input.Value);
        result.Should().Be(input.ExpectedExtension);
    }

    [Property(MaxTest = 50, Arbitrary = [typeof(ExtensionExtractionArbitraries)])]
    public void FilenameWithMultipleDots_ReturnsOnlyLastPart(MultiDotFilename input)
    {
        var result = DocumentService.ExtractExtension(input.Value);
        result.Should().Be(input.ExpectedExtension);
    }

    [Fact]
    public void NoDot_ReturnsEmpty()
    {
        DocumentService.ExtractExtension("filename").Should().BeEmpty();
    }

    [Fact]
    public void EndsWithDot_ReturnsEmpty()
    {
        DocumentService.ExtractExtension("filename.").Should().BeEmpty();
    }

    [Fact]
    public void StartsWithDot_ReturnsExtension()
    {
        DocumentService.ExtractExtension(".gitignore").Should().Be("gitignore");
    }

    [Fact]
    public void EmptyString_ReturnsEmpty()
    {
        DocumentService.ExtractExtension("").Should().BeEmpty();
    }

    [Fact]
    public void Null_ReturnsEmpty()
    {
        DocumentService.ExtractExtension(null!).Should().BeEmpty();
    }
}

#region Custom Types and Arbitraries for FsCheck 3.x

public record MixedCaseFilename(string Value);
public record SimpleFilenameWithExtension(string Value, string ExpectedExtension);
public record MultiDotFilename(string Value, string ExpectedExtension);

public class ExtensionExtractionArbitraries
{
    public static Arbitrary<MixedCaseFilename> MixedCaseFilenameArbitrary()
    {
        Gen<MixedCaseFilename> gen = FsCheck.Fluent.Gen.Select(
            FsCheck.Fluent.Gen.Elements("file.PDF", "doc.TXT", "image.Png", "data.CSV"),
            f => new MixedCaseFilename(f));
        return FsCheck.Fluent.Arb.From(gen);
    }

    public static Arbitrary<SimpleFilenameWithExtension> SimpleFilenameWithExtensionArbitrary()
    {
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(0, int.MaxValue);
        Gen<SimpleFilenameWithExtension> gen = FsCheck.Fluent.Gen.Select(seedGen, seed =>
        {
            var rng = new Random(seed);
            var names = new[] { "a", "bc", "def", "file", "document" };
            var extensions = new[] { "pdf", "txt", "png", "jpg", "docx" };
            var name = names[rng.Next(names.Length)];
            var ext = extensions[rng.Next(extensions.Length)];
            return new SimpleFilenameWithExtension($"{name}.{ext}", ext);
        });
        return FsCheck.Fluent.Arb.From(gen);
    }

    public static Arbitrary<MultiDotFilename> MultiDotFilenameArbitrary()
    {
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(0, int.MaxValue);
        Gen<MultiDotFilename> gen = FsCheck.Fluent.Gen.Select(seedGen, seed =>
        {
            var rng = new Random(seed);
            var prefixes = new[] { "archive.tar", "file.backup", "my.doc", "data.2024" };
            var extensions = new[] { "gz", "zip", "pdf", "bak" };
            var prefix = prefixes[rng.Next(prefixes.Length)];
            var ext = extensions[rng.Next(extensions.Length)];
            return new MultiDotFilename($"{prefix}.{ext}", ext);
        });
        return FsCheck.Fluent.Arb.From(gen);
    }
}

#endregion
