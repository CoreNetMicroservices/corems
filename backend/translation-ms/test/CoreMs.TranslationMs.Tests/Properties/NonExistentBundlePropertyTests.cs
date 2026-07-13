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
/// Property 3: Non-existent bundle returns 404
/// For any realm+lang where no bundle exists, GET, PUT, and DELETE operations all throw TranslationNotFound.
/// **Validates: Requirements 3.2, 5.2, 8.2, 9.2**
/// </summary>
public class NonExistentBundlePropertyTests
{
    [Property]
    public void GetTranslations_NonExistent_Returns404(NonEmptyString realm, NonEmptyString lang)
    {
        var dbContext = Substitute.For<DbContext>();
        var repository = Substitute.ForPartsOf<TranslationBundleRepository>(dbContext);
        var currentUserService = Substitute.For<ICurrentUserService>();
        var service = new TranslationService(repository, currentUserService);

        repository.GetByRealmAndLangAsync(realm.Get, lang.Get, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TranslationBundleEntity?>(null));

        var act = () => service.GetTranslationsAsync(realm.Get, lang.Get);
        var ex = act.Should().ThrowAsync<ServiceException>().Result.Which;
        ex.HttpStatusCode.Should().Be(404);
    }

    [Property]
    public void GetAdminTranslation_NonExistent_Returns404(NonEmptyString realm, NonEmptyString lang)
    {
        var dbContext = Substitute.For<DbContext>();
        var repository = Substitute.ForPartsOf<TranslationBundleRepository>(dbContext);
        var currentUserService = Substitute.For<ICurrentUserService>();
        var service = new TranslationService(repository, currentUserService);

        repository.GetByRealmAndLangAsync(realm.Get, lang.Get, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TranslationBundleEntity?>(null));

        var act = () => service.GetAdminTranslationAsync(realm.Get, lang.Get);
        var ex = act.Should().ThrowAsync<ServiceException>().Result.Which;
        ex.HttpStatusCode.Should().Be(404);
    }

    [Property]
    public void UpdateTranslation_NonExistent_Returns404(NonEmptyString realm, NonEmptyString lang)
    {
        var dbContext = Substitute.For<DbContext>();
        var repository = Substitute.ForPartsOf<TranslationBundleRepository>(dbContext);
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.GetCurrentUserUuid().Returns(Guid.NewGuid());
        var service = new TranslationService(repository, currentUserService);

        repository.GetByRealmAndLangAsync(realm.Get, lang.Get, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TranslationBundleEntity?>(null));

        var request = new TranslationRequest { Data = new Dictionary<string, string> { ["k"] = "v" } };
        var act = () => service.UpdateTranslationAsync(realm.Get, lang.Get, request);
        var ex = act.Should().ThrowAsync<ServiceException>().Result.Which;
        ex.HttpStatusCode.Should().Be(404);
    }

    [Property]
    public void DeleteTranslation_NonExistent_Returns404(NonEmptyString realm, NonEmptyString lang)
    {
        var dbContext = Substitute.For<DbContext>();
        var repository = Substitute.ForPartsOf<TranslationBundleRepository>(dbContext);
        var currentUserService = Substitute.For<ICurrentUserService>();
        var service = new TranslationService(repository, currentUserService);

        repository.GetByRealmAndLangAsync(realm.Get, lang.Get, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TranslationBundleEntity?>(null));

        var act = () => service.DeleteTranslationAsync(realm.Get, lang.Get);
        var ex = act.Should().ThrowAsync<ServiceException>().Result.Which;
        ex.HttpStatusCode.Should().Be(404);
    }
}
