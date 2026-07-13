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
/// Property 9: Update replaces bundle data
/// After PUT with new data, retrieval returns the new data (not original), realm and lang preserved.
/// **Validates: Requirements 8.1**
/// </summary>
public class UpdateReplacesBundleDataPropertyTests
{
    [Property(MaxTest = 100, Arbitrary = [typeof(TranslationArbitraries)])]
    public async Task Update_ReplacesData_PreservesRealmAndLang(TranslationInput input)
    {
        // Arrange
        var dbContext = Substitute.For<DbContext>();
        var repository = Substitute.ForPartsOf<TranslationBundleRepository>(dbContext);
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.GetCurrentUserUuid().Returns(Guid.NewGuid());
        var service = new TranslationService(repository, currentUserService);

        var originalData = new Dictionary<string, string> { ["original_key"] = "original_value" };
        var existingEntity = new TranslationBundleEntity
        {
            Id = 42,
            Realm = input.Realm,
            Lang = input.Lang,
            Data = originalData,
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedBy = Guid.NewGuid()
        };

        repository.GetByRealmAndLangAsync(input.Realm, input.Lang, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TranslationBundleEntity?>(existingEntity));

        var newData = input.Data;
        var request = new TranslationRequest { Data = newData };

        // Act
        var result = await service.UpdateTranslationAsync(input.Realm, input.Lang, request);

        // Assert: returned DTO has new data, not original
        result.Data.Should().BeEquivalentTo(newData);
        result.Data.Should().NotBeEquivalentTo(originalData);

        // Assert: realm and lang preserved
        result.Realm.Should().Be(input.Realm);
        result.Lang.Should().Be(input.Lang);

        // Assert: the entity was updated with new data
        existingEntity.Data.Should().BeEquivalentTo(newData);
    }
}
