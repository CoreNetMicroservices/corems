using CoreMs.CommunicationMs.Api.Validators;
using CoreMs.CommunicationMs.Core.Models;
using FluentAssertions;
using FluentValidation.Results;
using Xunit;

namespace CoreMs.CommunicationMs.Tests.Validators;

public class EmailMessageRequestValidatorTests
{
    private readonly EmailMessageRequestValidator _validator = new();

    private static EmailMessageRequest Valid() => new()
    {
        UserId = Guid.NewGuid(),
        Subject = "Hello",
        Recipient = "to@example.com",
        Body = "Body text"
    };

    [Fact]
    public void ValidRequest_Passes()
        => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Fact]
    public void EmptyUserId_Fails()
        => AssertInvalid(Valid() with { UserId = Guid.Empty }, nameof(EmailMessageRequest.UserId));

    [Fact]
    public void EmptySubject_Fails()
        => AssertInvalid(Valid() with { Subject = "" }, nameof(EmailMessageRequest.Subject));

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@")]
    [InlineData("")]
    public void InvalidRecipient_Fails(string recipient)
        => AssertInvalid(Valid() with { Recipient = recipient }, nameof(EmailMessageRequest.Recipient));

    [Theory]
    [InlineData("txt")]
    [InlineData("html")]
    public void ValidEmailType_Passes(string type)
        => _validator.Validate(Valid() with { EmailType = type }).IsValid.Should().BeTrue();

    [Fact]
    public void UnknownEmailType_Fails()
        => AssertInvalid(Valid() with { EmailType = "pdf" }, nameof(EmailMessageRequest.EmailType));

    [Fact]
    public void NeitherBodyNorTemplate_Fails()
    {
        var request = Valid() with { Body = null, Template = null };
        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void TemplateInsteadOfBody_Passes()
    {
        var request = Valid() with { Body = null, Template = new TemplateRequest { TemplateId = "welcome" } };
        _validator.Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void InvalidCcAddress_Fails()
        => AssertInvalid(Valid() with { Cc = ["ok@example.com", "bad"] }, "Cc");

    private void AssertInvalid(EmailMessageRequest request, string expectedProperty)
    {
        ValidationResult result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.StartsWith(expectedProperty));
    }
}

public class SmsNotificationRequestValidatorTests
{
    private readonly SmsNotificationRequestValidator _validator = new();

    private static SmsNotificationRequest Valid() => new()
    {
        PhoneNumber = "+15551234567",
        Message = "Hi"
    };

    [Fact]
    public void ValidRequest_Passes()
        => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData("15551234567")]   // missing +
    [InlineData("+")]              // no digits
    [InlineData("+abc")]           // non-digits
    [InlineData("")]
    public void InvalidPhoneNumber_Fails(string phone)
        => _validator.Validate(Valid() with { PhoneNumber = phone }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData("info")]
    [InlineData("warning")]
    [InlineData("critical")]
    public void ValidLevel_Passes(string level)
        => _validator.Validate(Valid() with { Level = level }).IsValid.Should().BeTrue();

    [Fact]
    public void UnknownLevel_Fails()
        => _validator.Validate(Valid() with { Level = "debug" }).IsValid.Should().BeFalse();

    [Fact]
    public void NeitherMessageNorTemplate_Fails()
        => _validator.Validate(Valid() with { Message = null, Template = null }).IsValid.Should().BeFalse();

    [Fact]
    public void TemplateInsteadOfMessage_Passes()
        => _validator.Validate(Valid() with { Message = null, Template = new TemplateRequest { TemplateId = "otp" } })
            .IsValid.Should().BeTrue();
}

public class SlackNotificationRequestValidatorTests
{
    private readonly SlackNotificationRequestValidator _validator = new();

    private static SlackNotificationRequest Valid() => new()
    {
        Channel = "#general",
        Message = "Deploy finished"
    };

    [Fact]
    public void ValidRequest_Passes()
        => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData("#general")]
    [InlineData("@alice")]
    public void ValidChannelPrefix_Passes(string channel)
        => _validator.Validate(Valid() with { Channel = channel }).IsValid.Should().BeTrue();

    [Theory]
    [InlineData("general")]  // no prefix
    [InlineData("")]
    public void InvalidChannel_Fails(string channel)
        => _validator.Validate(Valid() with { Channel = channel }).IsValid.Should().BeFalse();

    [Fact]
    public void EmptyMessage_Fails()
        => _validator.Validate(Valid() with { Message = "" }).IsValid.Should().BeFalse();

    [Fact]
    public void UnknownLevel_Fails()
        => _validator.Validate(Valid() with { Level = "trace" }).IsValid.Should().BeFalse();
}
