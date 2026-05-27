using CoreMs.Common.Exceptions;
using FluentAssertions;
using Xunit;

namespace CoreMs.Common.Tests;

public class ServiceExceptionTests
{
    [Fact]
    public void Of_WithErrorInfo_CreatesExceptionWithCorrectProperties()
    {
        var error = new ErrorInfo("user.not_found", 404, "User not found");

        var ex = ServiceException.Of(error);

        ex.HttpStatusCode.Should().Be(404);
        ex.Errors.Should().HaveCount(1);
        ex.Errors[0].ReasonCode.Should().Be("user.not_found");
        ex.Errors[0].Description.Should().Be("User not found");
        ex.Errors[0].Details.Should().BeNull();
        ex.Message.Should().Be("User not found");
    }

    [Fact]
    public void Of_WithDetails_IncludesDetailsInError()
    {
        var error = new ErrorInfo("user.not_found", 404, "User not found");

        var ex = ServiceException.Of(error, "User with ID xyz not found");

        ex.Errors[0].Details.Should().Be("User with ID xyz not found");
    }

    [Fact]
    public void Of_WithMultipleErrors_ReturnsAll()
    {
        var errors = new List<Error>
        {
            new("field.invalid", "Email is required"),
            new("field.invalid", "Password is required")
        };

        var ex = ServiceException.Of(errors, 400);

        ex.Errors.Should().HaveCount(2);
        ex.HttpStatusCode.Should().Be(400);
    }
}
