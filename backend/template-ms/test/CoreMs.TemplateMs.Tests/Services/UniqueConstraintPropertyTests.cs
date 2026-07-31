using CoreMs.Common.Exceptions;
using CoreMs.Common.Security;
using CoreMs.TemplateMs.Core.Entities;
using CoreMs.TemplateMs.Core.Models;
using CoreMs.TemplateMs.Core.Repositories;
using CoreMs.TemplateMs.Core.Services;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace CoreMs.TemplateMs.Tests.Services;

/// <summary>
/// Property 5: Unique constraint enforcement
/// For any two templates, if they share the same templateId and language,
/// the system should reject the second creation with a 409 conflict error,
/// regardless of other differing fields.
///
/// **Validates: Requirements 1.3, 8.1**
/// </summary>
public class UniqueConstraintPropertyTests
{
    [Property(MaxTest = 50, Arbitrary = [typeof(UniqueConstraintArbitraries)])]
    public void DuplicateCreate_Returns409_AndDoesNotPersist(DuplicateTemplateInput input)
    {
        // Arrange
        var dbContext = Substitute.For<DbContext>();
        var repository = Substitute.ForPartsOf<TemplateRepository>(dbContext);
        var engine = new TemplateEngine();
        var cache = new TemplateCache();
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.GetCurrentUserUuid().Returns(Guid.NewGuid());

        var service = new TemplateService(repository, engine, cache, currentUserService);

        // Setup: a template with this templateId+language already exists
        repository.ExistsByTemplateIdAndLanguageAsync(input.TemplateId, input.Language, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var request = new CreateTemplateRequest
        {
            TemplateId = input.TemplateId,
            Language = input.Language,
            Name = input.Name,
            Content = "Hello {{name}}",
            Category = "COMMON"
        };

        // Act
        var act = () => service.CreateAsync(request);

        // Assert: ServiceException with 409 status
        var ex = act.Should().ThrowAsync<ServiceException>().Result.Which;
        ex.HttpStatusCode.Should().Be(409);

        // Assert: repository.Add was never called (no duplicate persisted)
        repository.DidNotReceive().Add(Arg.Any<TemplateEntity>());
    }

    [Property(MaxTest = 50, Arbitrary = [typeof(UniqueConstraintArbitraries)])]
    public void UniqueCreate_Succeeds_WhenNoExistingTemplate(DuplicateTemplateInput input)
    {
        // Arrange
        var dbContext = Substitute.For<DbContext>();
        var repository = Substitute.ForPartsOf<TemplateRepository>(dbContext);
        var engine = new TemplateEngine();
        var cache = new TemplateCache();
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.GetCurrentUserUuid().Returns(Guid.NewGuid());

        var service = new TemplateService(repository, engine, cache, currentUserService);

        // Setup: no template with this templateId+language exists
        repository.ExistsByTemplateIdAndLanguageAsync(input.TemplateId, input.Language, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var request = new CreateTemplateRequest
        {
            TemplateId = input.TemplateId,
            Language = input.Language,
            Name = input.Name,
            Content = "Hello {{name}}",
            Category = "COMMON"
        };

        // Act
        var result = service.CreateAsync(request).GetAwaiter().GetResult();

        // Assert: succeeds and returns the correct templateId+language
        result.TemplateId.Should().Be(input.TemplateId);
        result.Language.Should().Be(input.Language);

        // Assert: repository.Add was called exactly once
        repository.Received(1).Add(Arg.Any<TemplateEntity>());
    }
}

#region Arbitraries

public record DuplicateTemplateInput(string TemplateId, string Language, string Name)
{
    public override string ToString() => $"{TemplateId}:{Language} ({Name})";
}

public class UniqueConstraintArbitraries
{
    public static Arbitrary<DuplicateTemplateInput> DuplicateTemplateInputArbitrary()
    {
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(0, int.MaxValue);
        Gen<DuplicateTemplateInput> gen = FsCheck.Fluent.Gen.Select(seedGen, seed =>
        {
            var rng = new Random(seed);
            var templateIds = new[] { "welcome-email", "password-reset", "invoice", "notification", "sms-verify" };
            var languages = new[] { "en", "fr", "de", "es", "pt" };
            var names = new[] { "Welcome Email", "Reset Password", "Invoice Template", "Alert", "Verification" };
            return new DuplicateTemplateInput(
                templateIds[rng.Next(templateIds.Length)],
                languages[rng.Next(languages.Length)],
                names[rng.Next(names.Length)]);
        });
        return FsCheck.Fluent.Arb.From(gen);
    }
}

#endregion
