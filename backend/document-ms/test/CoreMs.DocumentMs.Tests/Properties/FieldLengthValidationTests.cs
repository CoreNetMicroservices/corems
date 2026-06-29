using CoreMs.DocumentMs.Api.Validators;
using CoreMs.DocumentMs.Core.Models;
using FsCheck;
using FsCheck.Xunit;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace CoreMs.DocumentMs.Tests.Properties;

/// <summary>
/// Property 14: Field length validation — strings exceeding max length are rejected;
/// within bounds are accepted.
///
/// **Validates: Requirements 13.1, 13.2, 13.3, 13.4**
/// </summary>
public class FieldLengthValidationTests
{
    private readonly UploadDocumentRequestValidator _uploadValidator = new();
    private readonly UploadBase64RequestValidator _base64Validator = new();
    private readonly UpdateDocumentRequestValidator _updateValidator = new();

    [Fact]
    public void Name_Within255_IsAccepted()
    {
        var request = new UploadDocumentRequest(new string('a', 255), null, null, null);
        var result = _uploadValidator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Name_Exceeds255_IsRejected()
    {
        var request = new UploadDocumentRequest(new string('a', 256), null, null, null);
        var result = _uploadValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Description_Within2000_IsAccepted()
    {
        var request = new UploadDocumentRequest(null, new string('b', 2000), null, null);
        var result = _uploadValidator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Description_Exceeds2000_IsRejected()
    {
        var request = new UploadDocumentRequest(null, new string('b', 2001), null, null);
        var result = _uploadValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Base64_FileName_Within500_IsAccepted()
    {
        var request = new UploadBase64Request(new string('c', 496) + ".pdf", "dGVzdA==", null, null, null, null);
        var result = _base64Validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.FileName);
    }

    [Fact]
    public void Base64_FileName_Exceeds500_IsRejected()
    {
        var request = new UploadBase64Request(new string('c', 497) + ".pdf", "dGVzdA==", null, null, null, null);
        var result = _base64Validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Fact]
    public void Base64_FileName_Empty_IsRejected()
    {
        var request = new UploadBase64Request("", "dGVzdA==", null, null, null, null);
        var result = _base64Validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Fact]
    public void Update_Name_Within255_IsAccepted()
    {
        var request = new UpdateDocumentRequest(new string('d', 255), null, null, null);
        var result = _updateValidator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Update_Name_Exceeds255_IsRejected()
    {
        var request = new UpdateDocumentRequest(new string('d', 256), null, null, null);
        var result = _updateValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void NullFields_AreAccepted()
    {
        var request = new UploadDocumentRequest(null, null, null, null);
        var result = _uploadValidator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
