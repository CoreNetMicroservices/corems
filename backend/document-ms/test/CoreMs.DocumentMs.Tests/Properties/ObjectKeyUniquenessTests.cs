using System.Reflection;
using FsCheck;
using FsCheck.Xunit;
using Xunit;
using CoreMs.DocumentMs.Core.Services;

namespace CoreMs.DocumentMs.Tests.Properties;

/// <summary>
/// Property 9: Object key uniqueness — For any set of uploads, all generated
/// Object_Key values are distinct regardless of file content, filename, or user.
///
/// **Validates: Requirements 1.3, 13.5**
/// </summary>
public class ObjectKeyUniquenessTests
{
    /// <summary>
    /// For any count between 2 and 20, generating multiple object keys with the same
    /// userId and filename always produces distinct values.
    /// Validates: Requirements 1.3, 13.5
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = [typeof(ObjectKeyArbitraries)])]
    public void MultipleUploads_ProduceDistinctObjectKeys(UploadCount count)
    {
        var userId = Guid.NewGuid();
        var filename = "test-file.pdf";

        var keys = Enumerable.Range(0, count.Value)
            .Select(_ => InvokeGenerateObjectKey(userId, filename))
            .ToList();

        Assert.Equal(keys.Count, keys.Distinct().Count());
    }

    /// <summary>
    /// For any count between 2 and 10, generating object keys with same filename
    /// but different users always produces distinct values.
    /// Validates: Requirements 1.3, 13.5
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = [typeof(ObjectKeyArbitraries)])]
    public void SameFilename_DifferentUsers_ProduceDistinctKeys(SmallUploadCount count)
    {
        var filename = "document.pdf";
        var keys = Enumerable.Range(0, count.Value)
            .Select(_ => InvokeGenerateObjectKey(Guid.NewGuid(), filename))
            .ToList();

        Assert.Equal(keys.Count, keys.Distinct().Count());
    }

    [Fact]
    public void ObjectKey_ContainsUserId()
    {
        var userId = Guid.NewGuid();
        var key = InvokeGenerateObjectKey(userId, "test.pdf");

        Assert.StartsWith(userId.ToString(), key);
    }

    [Fact]
    public void ObjectKey_ContainsFileExtension()
    {
        var key = InvokeGenerateObjectKey(Guid.NewGuid(), "document.pdf");

        Assert.EndsWith(".pdf", key);
    }

    private static string InvokeGenerateObjectKey(Guid userId, string filename)
    {
        var method = typeof(DocumentService).GetMethod("GenerateObjectKey",
            BindingFlags.NonPublic | BindingFlags.Static);
        return (string)method!.Invoke(null, [userId, filename])!;
    }
}

#region Custom Types and Arbitraries for FsCheck 3.x

public record UploadCount(int Value);
public record SmallUploadCount(int Value);

public class ObjectKeyArbitraries
{
    public static Arbitrary<UploadCount> UploadCountArbitrary()
    {
        var gen = FsCheck.Fluent.Gen.Select(
            FsCheck.Fluent.Gen.Choose(2, 20),
            n => new UploadCount(n));
        return FsCheck.Fluent.Arb.From(gen);
    }

    public static Arbitrary<SmallUploadCount> SmallUploadCountArbitrary()
    {
        var gen = FsCheck.Fluent.Gen.Select(
            FsCheck.Fluent.Gen.Choose(2, 10),
            n => new SmallUploadCount(n));
        return FsCheck.Fluent.Arb.From(gen);
    }
}

#endregion
