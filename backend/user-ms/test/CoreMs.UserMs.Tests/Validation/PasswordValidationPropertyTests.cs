using CoreMs.UserMs.Api.Validators;
using CoreMs.UserMs.Core.Models;
using FluentValidation;
using FluentValidation.Results;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace CoreMs.UserMs.Tests.Validation;

/// <summary>
/// Property 4: Password Validation Rejects Weak Passwords
/// For any password string that is shorter than 8 characters, or missing an uppercase letter,
/// lowercase letter, digit, or special character, the Validation Pipeline SHALL reject the request.
///
/// Property 5: Field Length Validation
/// For any request DTO field value exceeding its configured maximum length,
/// the Validation Pipeline SHALL reject the request with HTTP 400.
///
/// **Validates: Requirements 2.1, 2.2, 2.3, 2.4, 2.5**
/// </summary>
public class PasswordValidationPropertyTests
{
    private readonly SignUpRequestValidator _validator = new();

    private static SignUpRequest CreateRequest(string password) => new(
        Email: "valid@example.com",
        Password: password,
        ConfirmPassword: password,
        FirstName: "John",
        LastName: "Doe",
        PhoneNumber: null,
        ImageUrl: null);

    /// <summary>
    /// Property 4: Any password meeting ALL complexity rules passes validation.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = [typeof(PasswordArbitraries)])]
    public void ValidPassword_ShouldPassValidation(ValidPassword password)
    {
        var request = CreateRequest(password.Value);
        var result = _validator.Validate(request);

        Assert.True(result.IsValid,
            $"Expected valid but got errors for '{password.Value}': {FormatErrors(result)}");
    }

    /// <summary>
    /// Property 4: Any password shorter than 8 characters fails validation.
    /// Validates: Requirement 2.2
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = [typeof(PasswordArbitraries)])]
    public void PasswordTooShort_ShouldFailValidation(ShortPassword password)
    {
        var request = CreateRequest(password.Value);
        var result = _validator.Validate(request);

        Assert.False(result.IsValid,
            $"Expected invalid but passed for short password: '{password.Value}'");
    }

    /// <summary>
    /// Property 4: Any password missing an uppercase letter fails validation.
    /// Validates: Requirement 2.3
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = [typeof(PasswordArbitraries)])]
    public void PasswordMissingUppercase_ShouldFailValidation(NoUppercasePassword password)
    {
        var request = CreateRequest(password.Value);
        var result = _validator.Validate(request);

        Assert.False(result.IsValid,
            $"Expected invalid but passed for password without uppercase: '{password.Value}'");
    }

    /// <summary>
    /// Property 4: Any password missing a lowercase letter fails validation.
    /// Validates: Requirement 2.3
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = [typeof(PasswordArbitraries)])]
    public void PasswordMissingLowercase_ShouldFailValidation(NoLowercasePassword password)
    {
        var request = CreateRequest(password.Value);
        var result = _validator.Validate(request);

        Assert.False(result.IsValid,
            $"Expected invalid but passed for password without lowercase: '{password.Value}'");
    }

    /// <summary>
    /// Property 4: Any password missing a digit fails validation.
    /// Validates: Requirement 2.3
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = [typeof(PasswordArbitraries)])]
    public void PasswordMissingDigit_ShouldFailValidation(NoDigitPassword password)
    {
        var request = CreateRequest(password.Value);
        var result = _validator.Validate(request);

        Assert.False(result.IsValid,
            $"Expected invalid but passed for password without digit: '{password.Value}'");
    }

    /// <summary>
    /// Property 4: Any password missing a special character fails validation.
    /// Validates: Requirement 2.3
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = [typeof(PasswordArbitraries)])]
    public void PasswordMissingSpecialChar_ShouldFailValidation(NoSpecialCharPassword password)
    {
        var request = CreateRequest(password.Value);
        var result = _validator.Validate(request);

        Assert.False(result.IsValid,
            $"Expected invalid but passed for password without special char: '{password.Value}'");
    }

    /// <summary>
    /// Property 5: Any firstName exceeding 50 characters fails validation.
    /// Validates: Requirement 2.4
    /// </summary>
    [Property(MaxTest = 50, Arbitrary = [typeof(PasswordArbitraries)])]
    public void FirstNameExceedingMaxLength_ShouldFailValidation(OverlongField field)
    {
        var request = new SignUpRequest(
            Email: "valid@example.com",
            Password: "Valid1Pass!",
            ConfirmPassword: "Valid1Pass!",
            FirstName: field.Value,
            LastName: "Doe",
            PhoneNumber: null,
            ImageUrl: null);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid,
            $"Expected invalid but passed for firstName with length {field.Value.Length}");
    }

    /// <summary>
    /// Property 5: Any lastName exceeding 50 characters fails validation.
    /// Validates: Requirement 2.4
    /// </summary>
    [Property(MaxTest = 50, Arbitrary = [typeof(PasswordArbitraries)])]
    public void LastNameExceedingMaxLength_ShouldFailValidation(OverlongField field)
    {
        var request = new SignUpRequest(
            Email: "valid@example.com",
            Password: "Valid1Pass!",
            ConfirmPassword: "Valid1Pass!",
            FirstName: "John",
            LastName: field.Value,
            PhoneNumber: null,
            ImageUrl: null);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid,
            $"Expected invalid but passed for lastName with length {field.Value.Length}");
    }

    private static string FormatErrors(ValidationResult result)
        => string.Join("; ", result.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
}

#region Custom Types and Arbitraries

public record ValidPassword(string Value)
{
    public override string ToString() => Value;
}

public record ShortPassword(string Value)
{
    public override string ToString() => Value;
}

public record NoUppercasePassword(string Value)
{
    public override string ToString() => Value;
}

public record NoLowercasePassword(string Value)
{
    public override string ToString() => Value;
}

public record NoDigitPassword(string Value)
{
    public override string ToString() => Value;
}

public record NoSpecialCharPassword(string Value)
{
    public override string ToString() => Value;
}

public record OverlongField(string Value)
{
    public override string ToString() => $"[{Value.Length} chars]";
}

public class PasswordArbitraries
{
    private const string UpperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string LowerChars = "abcdefghijklmnopqrstuvwxyz";
    private const string DigitChars = "0123456789";
    private const string SpecialCharsStr = "!@#$%^&*()_+-=[]{}|;:,.<>?/~";

    public static Arbitrary<ValidPassword> ValidPasswordArbitrary()
    {
        // Use a seed integer to deterministically build a valid password
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(0, int.MaxValue);
        Gen<ValidPassword> gen = FsCheck.Fluent.Gen.Select(seedGen, seed =>
        {
            var rng = new Random(seed);
            // Ensure at least 2 of each category = 8+ chars
            var chars = new char[10];
            chars[0] = UpperChars[rng.Next(UpperChars.Length)];
            chars[1] = UpperChars[rng.Next(UpperChars.Length)];
            chars[2] = LowerChars[rng.Next(LowerChars.Length)];
            chars[3] = LowerChars[rng.Next(LowerChars.Length)];
            chars[4] = DigitChars[rng.Next(DigitChars.Length)];
            chars[5] = DigitChars[rng.Next(DigitChars.Length)];
            chars[6] = SpecialCharsStr[rng.Next(SpecialCharsStr.Length)];
            chars[7] = SpecialCharsStr[rng.Next(SpecialCharsStr.Length)];
            // 2 more random chars from all sets
            var all = UpperChars + LowerChars + DigitChars + SpecialCharsStr;
            chars[8] = all[rng.Next(all.Length)];
            chars[9] = all[rng.Next(all.Length)];
            // Shuffle
            for (int i = chars.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }
            return new ValidPassword(new string(chars));
        });

        return FsCheck.Fluent.Arb.From(gen);
    }

    public static Arbitrary<ShortPassword> ShortPasswordArbitrary()
    {
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(0, int.MaxValue);
        Gen<ShortPassword> gen = FsCheck.Fluent.Gen.Select(seedGen, seed =>
        {
            var rng = new Random(seed);
            var length = rng.Next(1, 8); // 1 to 7
            var all = UpperChars + LowerChars + DigitChars + SpecialCharsStr;
            var chars = new char[length];
            for (int i = 0; i < length; i++)
                chars[i] = all[rng.Next(all.Length)];
            return new ShortPassword(new string(chars));
        });

        return FsCheck.Fluent.Arb.From(gen);
    }

    public static Arbitrary<NoUppercasePassword> NoUppercasePasswordArbitrary()
    {
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(0, int.MaxValue);
        Gen<NoUppercasePassword> gen = FsCheck.Fluent.Gen.Select(seedGen, seed =>
        {
            var rng = new Random(seed);
            var allowed = LowerChars + DigitChars + SpecialCharsStr;
            var length = rng.Next(8, 16);
            var chars = new char[length];
            // Ensure at least one lowercase, one digit, one special
            chars[0] = LowerChars[rng.Next(LowerChars.Length)];
            chars[1] = DigitChars[rng.Next(DigitChars.Length)];
            chars[2] = SpecialCharsStr[rng.Next(SpecialCharsStr.Length)];
            for (int i = 3; i < length; i++)
                chars[i] = allowed[rng.Next(allowed.Length)];
            // Shuffle
            for (int i = chars.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }
            return new NoUppercasePassword(new string(chars));
        });

        return FsCheck.Fluent.Arb.From(gen);
    }

    public static Arbitrary<NoLowercasePassword> NoLowercasePasswordArbitrary()
    {
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(0, int.MaxValue);
        Gen<NoLowercasePassword> gen = FsCheck.Fluent.Gen.Select(seedGen, seed =>
        {
            var rng = new Random(seed);
            var allowed = UpperChars + DigitChars + SpecialCharsStr;
            var length = rng.Next(8, 16);
            var chars = new char[length];
            // Ensure at least one uppercase, one digit, one special
            chars[0] = UpperChars[rng.Next(UpperChars.Length)];
            chars[1] = DigitChars[rng.Next(DigitChars.Length)];
            chars[2] = SpecialCharsStr[rng.Next(SpecialCharsStr.Length)];
            for (int i = 3; i < length; i++)
                chars[i] = allowed[rng.Next(allowed.Length)];
            // Shuffle
            for (int i = chars.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }
            return new NoLowercasePassword(new string(chars));
        });

        return FsCheck.Fluent.Arb.From(gen);
    }

    public static Arbitrary<NoDigitPassword> NoDigitPasswordArbitrary()
    {
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(0, int.MaxValue);
        Gen<NoDigitPassword> gen = FsCheck.Fluent.Gen.Select(seedGen, seed =>
        {
            var rng = new Random(seed);
            var allowed = UpperChars + LowerChars + SpecialCharsStr;
            var length = rng.Next(8, 16);
            var chars = new char[length];
            // Ensure at least one uppercase, one lowercase, one special
            chars[0] = UpperChars[rng.Next(UpperChars.Length)];
            chars[1] = LowerChars[rng.Next(LowerChars.Length)];
            chars[2] = SpecialCharsStr[rng.Next(SpecialCharsStr.Length)];
            for (int i = 3; i < length; i++)
                chars[i] = allowed[rng.Next(allowed.Length)];
            // Shuffle
            for (int i = chars.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }
            return new NoDigitPassword(new string(chars));
        });

        return FsCheck.Fluent.Arb.From(gen);
    }

    public static Arbitrary<NoSpecialCharPassword> NoSpecialCharPasswordArbitrary()
    {
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(0, int.MaxValue);
        Gen<NoSpecialCharPassword> gen = FsCheck.Fluent.Gen.Select(seedGen, seed =>
        {
            var rng = new Random(seed);
            var allowed = UpperChars + LowerChars + DigitChars;
            var length = rng.Next(8, 16);
            var chars = new char[length];
            // Ensure at least one uppercase, one lowercase, one digit
            chars[0] = UpperChars[rng.Next(UpperChars.Length)];
            chars[1] = LowerChars[rng.Next(LowerChars.Length)];
            chars[2] = DigitChars[rng.Next(DigitChars.Length)];
            for (int i = 3; i < length; i++)
                chars[i] = allowed[rng.Next(allowed.Length)];
            // Shuffle
            for (int i = chars.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }
            return new NoSpecialCharPassword(new string(chars));
        });

        return FsCheck.Fluent.Arb.From(gen);
    }

    public static Arbitrary<OverlongField> OverlongFieldArbitrary()
    {
        Gen<int> lengthGen = FsCheck.Fluent.Gen.Choose(51, 200);
        Gen<OverlongField> gen = FsCheck.Fluent.Gen.Select(lengthGen,
            length => new OverlongField(new string('a', length)));

        return FsCheck.Fluent.Arb.From(gen);
    }
}

#endregion
