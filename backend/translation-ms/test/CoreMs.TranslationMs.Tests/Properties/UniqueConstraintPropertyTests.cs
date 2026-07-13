using CoreMs.Common.Exceptions;
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
/// Property 2: Unique constraint enforcement
/// For any realm+lang where a bundle already exists, creating another returns 409 and original data is unchanged.
/// **Validates: Requirements 2.2, 7.3**
/// </summary>
public class UniqueConstraintPropertyTests
{
    [Property]
    public void DuplicateCreate_Returns409_AndDoesNotAdd(NonEmptyString realm, NonEmptyString lang)
    {
        // Arrange
        var dbContext = Substitute.For<DbContext>();
        var repository = Substitute.ForPartsOf<TranslationBundleRepository>(dbContext);
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.GetCurrentUserUuid().Returns(Guid.NewGuid());
        var service = new TranslationService(repository, currentUserService);

        var realmValue = realm.Get;
        var langValue = lang.Get;

        // Setup: bundle already exists for this realm+lang
        repository.ExistsByRealmAndLangAsync(realmValue, langValue, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var request = new TranslationRequest { Data = new Dictionary<string, string> { ["key"] = "value" } };

        // Act
        var act = () => service.CreateTranslationAsync(realmValue, langValue, request);

        // Assert: ServiceException with 409 status
        var ex = act.Should().ThrowAsync<ServiceException>().Result.Which;
        ex.HttpStatusCode.Should().Be(409);

        // Assert: repository.Add was never called (original data unchanged)
        repository.DidNotReceive().Add(Arg.Any<TranslationBundleEntity>());
    }
}
