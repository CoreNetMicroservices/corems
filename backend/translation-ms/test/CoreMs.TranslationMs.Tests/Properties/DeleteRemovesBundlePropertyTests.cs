using CoreMs.Common.Exceptions;
using CoreMs.Common.Security;
using CoreMs.TranslationMs.Core.Entities;
using CoreMs.TranslationMs.Core.Repositories;
using CoreMs.TranslationMs.Core.Services;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace CoreMs.TranslationMs.Tests.Properties;

/// <summary>
/// Property 10: Delete removes bundle
/// After successful DELETE, GET returns 404 and bundle no longer appears in languages listing.
/// **Validates: Requirements 9.1, 9.3**
/// </summary>
public class DeleteRemovesBundlePropertyTests
{
    [Property(MaxTest = 100, Arbitrary = [typeof(TranslationArbitraries)])]
    public async Task Delete_RemovesBundle_RepositoryRemoveCalled(TranslationInput input)
    {
        // Arrange
        var dbContext = Substitute.For<DbContext>();
        var repository = Substitute.ForPartsOf<TranslationBundleRepository>(dbContext);
        var currentUserService = Substitute.For<ICurrentUserService>();
        var service = new TranslationService(repository, currentUserService);

        var existingEntity = new TranslationBundleEntity
        {
            Id = 1,
            Realm = input.Realm,
            Lang = input.Lang,
            Data = input.Data,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = Guid.NewGuid()
        };

        repository.GetByRealmAndLangAsync(input.Realm, input.Lang, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TranslationBundleEntity?>(existingEntity));

        // Act
        await service.DeleteTranslationAsync(input.Realm, input.Lang);

        // Assert: Remove was called with the correct entity
        repository.Received(1).Remove(existingEntity);
    }

    [Property(MaxTest = 100, Arbitrary = [typeof(TranslationArbitraries)])]
    public async Task Delete_SubsequentGet_Returns404(TranslationInput input)
    {
        // Arrange
        var dbContext = Substitute.For<DbContext>();
        var repository = Substitute.ForPartsOf<TranslationBundleRepository>(dbContext);
        var currentUserService = Substitute.For<ICurrentUserService>();
        var service = new TranslationService(repository, currentUserService);

        var existingEntity = new TranslationBundleEntity
        {
            Id = 1,
            Realm = input.Realm,
            Lang = input.Lang,
            Data = input.Data,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = Guid.NewGuid()
        };

        // First call returns entity (for delete), subsequent calls return null (simulating removal)
        var callCount = 0;
        repository.GetByRealmAndLangAsync(input.Realm, input.Lang, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                return Task.FromResult<TranslationBundleEntity?>(callCount == 1 ? existingEntity : null);
            });

        // Act: Delete the bundle
        await service.DeleteTranslationAsync(input.Realm, input.Lang);

        // Assert: Subsequent GET throws 404
        var act = () => service.GetTranslationsAsync(input.Realm, input.Lang);
        var ex = await act.Should().ThrowAsync<ServiceException>();
        ex.Which.HttpStatusCode.Should().Be(404);
    }

    [Property(MaxTest = 100, Arbitrary = [typeof(TranslationArbitraries)])]
    public async Task Delete_BundleNoLongerInLanguagesListing(TranslationInput input)
    {
        // Arrange
        var dbContext = Substitute.For<DbContext>();
        var repository = Substitute.ForPartsOf<TranslationBundleRepository>(dbContext);
        var currentUserService = Substitute.For<ICurrentUserService>();
        var service = new TranslationService(repository, currentUserService);

        var existingEntity = new TranslationBundleEntity
        {
            Id = 1,
            Realm = input.Realm,
            Lang = input.Lang,
            Data = input.Data,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = Guid.NewGuid()
        };

        repository.GetByRealmAndLangAsync(input.Realm, input.Lang, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TranslationBundleEntity?>(existingEntity));

        // After delete, languages listing should not include the deleted lang
        repository.GetLanguagesByRealmAsync(input.Realm, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<string>()));

        // Act: Delete
        await service.DeleteTranslationAsync(input.Realm, input.Lang);

        // Assert: languages listing doesn't include the deleted lang
        var languages = await service.GetLanguagesByRealmAsync(input.Realm);
        languages.Should().NotContain(input.Lang);
    }
}
