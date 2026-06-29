using System.Reflection;
using System.Runtime.CompilerServices;
using CoreMs.Common.Exceptions;
using CoreMs.DocumentMs.Core.Configuration;
using CoreMs.DocumentMs.Core.Services;
using FsCheck;
using FsCheck.Xunit;
using FluentAssertions;
using Xunit;

namespace CoreMs.DocumentMs.Tests.Properties;

/// <summary>
/// **Validates: Requirements 1.6**
/// Property 10: Extension validation rejects disallowed file types — For any extension
/// not in the allowed list, upload is rejected; for any in the list, upload is accepted.
/// </summary>
public class ExtensionValidationTests
{
    private static readonly string[] AllowedExtensions =
        ["pdf", "png", "jpg", "jpeg", "gif", "doc", "docx", "xls", "xlsx", "txt", "csv", "zip"];

    [Property(MaxTest = 50, Arbitrary = [typeof(ExtensionValidationArbitraries)])]
    public void AllowedExtension_IsAccepted(AllowedExtensionInput input)
    {
        var act = () => InvokeValidateExtension(input.Value);
        act.Should().NotThrow();
    }

    [Property(MaxTest = 50, Arbitrary = [typeof(ExtensionValidationArbitraries)])]
    public void DisallowedExtension_IsRejected(DisallowedExtensionInput input)
    {
        var act = () => InvokeValidateExtension(input.Value);
        act.Should().Throw<TargetInvocationException>()
            .WithInnerException<ServiceException>();
    }

    [Fact]
    public void EmptyExtension_IsAccepted()
    {
        var act = () => InvokeValidateExtension("");
        act.Should().NotThrow();
    }

    [Fact]
    public void AllowedExtension_CaseInsensitive()
    {
        var act = () => InvokeValidateExtension("PDF");
        act.Should().NotThrow();
    }

    private static void InvokeValidateExtension(string extension)
    {
        var options = new DocumentOptions
        {
            AllowedExtensions = AllowedExtensions
        };

        var instance = CreateMinimalDocumentService(options);

        var method = typeof(DocumentService).GetMethod("ValidateExtension",
            BindingFlags.NonPublic | BindingFlags.Instance);

        method!.Invoke(instance, [extension]);
    }

    private static DocumentService CreateMinimalDocumentService(DocumentOptions options)
    {
        var instance = (DocumentService)RuntimeHelpers.GetUninitializedObject(typeof(DocumentService));
        var field = typeof(DocumentService).GetField("_documentOptions", BindingFlags.NonPublic | BindingFlags.Instance);
        field!.SetValue(instance, options);
        return instance;
    }
}

#region Custom Types and Arbitraries for FsCheck 3.x

public record AllowedExtensionInput(string Value);
public record DisallowedExtensionInput(string Value);

public class ExtensionValidationArbitraries
{
    private static readonly string[] Allowed =
        ["pdf", "png", "jpg", "jpeg", "gif", "doc", "docx", "xls", "xlsx", "txt", "csv", "zip"];

    private static readonly string[] Disallowed =
        ["exe", "bat", "cmd", "sh", "dll", "so", "bin", "msi", "ps1", "vbs"];

    public static Arbitrary<AllowedExtensionInput> AllowedExtensionInputArbitrary()
    {
        Gen<AllowedExtensionInput> gen = FsCheck.Fluent.Gen.Select(
            FsCheck.Fluent.Gen.Elements(Allowed),
            ext => new AllowedExtensionInput(ext));
        return FsCheck.Fluent.Arb.From(gen);
    }

    public static Arbitrary<DisallowedExtensionInput> DisallowedExtensionInputArbitrary()
    {
        Gen<DisallowedExtensionInput> gen = FsCheck.Fluent.Gen.Select(
            FsCheck.Fluent.Gen.Elements(Disallowed),
            ext => new DisallowedExtensionInput(ext));
        return FsCheck.Fluent.Arb.From(gen);
    }
}

#endregion
