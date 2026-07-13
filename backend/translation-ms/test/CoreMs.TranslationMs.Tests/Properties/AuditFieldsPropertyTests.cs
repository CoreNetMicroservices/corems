using CoreMs.Common.Security;
using CoreMs.TranslationMs.Core.Entities;
using CoreMs.TranslationMs.Core.Models;
using CoreMs.TranslationMs.Core.Repositories;
using CoreMs.TranslationMs.Core.Services;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace CoreMs.TranslationMs.Tests.Properties;

/// <summary>
/// Property 8: Audit fields set on write operations
/// For any create/update, UpdatedAt is within tolerance of UTC now,
/// and UpdatedBy matches the authenticated user's UUID.
/// **Validates: Requirements 7.5, 7.6, 8.4, 8.5**
/// </summary>
public class AuditFieldsPropertyTests
{
    [Property(MaxTest = 100, Arbitrary = [typeof(TranslationArbitraries)])]
    public void Create_SetsAuditFields(TranslationInput input)
    {
        // Arrange
        var dbContext = Substitute.For<DbContext>();
        var repository = Substitute.ForPartsOf<TranslationBundleRepository>(dbContext);
        var currentUserService = Substitute.For<ICurrentUserService>();
        var userId = Guid.NewGuid();
        currentUserService.GetCurrentUserUuid().Returns(userId);
        var service = new TranslationService(repository, currentUserService);

        repository.ExistsByRealmAndLangAsync(input.Realm, input.Lang, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        TranslationBundleEntity? capturedEntity = null;
        repository.When(r => r.Add(Arg.Any<TranslationBundleEntity>()))
            .Do(ci => capturedEntity = ci.Arg<TranslationBundleEntity>());

        var request = new TranslationRequest { Data = input.Data };
        var beforeCall = DateTime.UtcNow;

        // Act
        var result = service.CreateTranslationAsync(input.Realm, input.Lang, request).Result;

        var afterCall = DateTime.UtcNow;

        // Assert
        capturedEntity.Should().NotBeNull();
        capturedEntity!.UpdatedAt.Should().BeOnOrAfter(beforeCall).And.BeOnOrBefore(afterCall);
        capturedEntity.UpdatedBy.Should().Be(userId);
        result.UpdatedBy.Should().Be(userId);
    }

    [Property(MaxTest = 100, Arbitrary = [typeof(TranslationArbitraries)])]
    public void Update_SetsAuditFields(TranslationInput input)
    {
        // Arrange
        var dbContext = Substitute.For<DbContext>();
        var repository = Substitute.ForPartsOf<TranslationBundleRepository>(dbContext);
        var currentUserService = Substitute.For<ICurrentUserService>();
        var userId = Guid.NewGuid();
        currentUserService.GetCurrentUserUuid().Returns(userId);
        var service = new TranslationService(repository, currentUserService);

        var existingEntity = new TranslationBundleEntity
        {
            Id = 1,
            Realm = input.Realm,
            Lang = input.Lang,
            Data = new Dictionary<string, string> { ["old"] = "data" },
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedBy = Guid.NewGuid()
        };

        repository.GetByRealmAndLangAsync(input.Realm, input.Lang, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TranslationBundleEntity?>(existingEntity));

        var request = new TranslationRequest { Data = input.Data };
        var beforeCall = DateTime.UtcNow;

        // Act
        var result = service.UpdateTranslationAsync(input.Realm, input.Lang, request).Result;

        var afterCall = DateTime.UtcNow;

        // Assert
        existingEntity.UpdatedAt.Should().BeOnOrAfter(beforeCall).And.BeOnOrBefore(afterCall);
        existingEntity.UpdatedBy.Should().Be(userId);
        result.UpdatedBy.Should().Be(userId);
    }
}
