using CoreMs.Common.Exceptions;
using CoreMs.Common.Extensions;
using CoreMs.Common.Repository;
using CoreMs.Common.Security;
using CoreMs.TranslationMs.Core.Entities;
using CoreMs.TranslationMs.Core.Exceptions;
using CoreMs.TranslationMs.Core.Models;
using CoreMs.TranslationMs.Core.Repositories;

namespace CoreMs.TranslationMs.Core.Services;

[Service]
public class TranslationService(
    TranslationBundleRepository repository,
    ICurrentUserService currentUserService)
{
    // === Public retrieval methods ===

    public virtual async Task<Dictionary<string, string>> GetTranslationsAsync(
        string realm, string lang, CancellationToken ct = default)
    {
        var entity = await repository.GetByRealmAndLangAsync(realm, lang, ct)
            ?? throw ServiceException.Of(TranslationServiceErrors.TranslationNotFound);
        return entity.Data;
    }

    public virtual async Task<List<string>> GetLanguagesByRealmAsync(
        string realm, CancellationToken ct = default)
        => await repository.GetLanguagesByRealmAsync(realm, ct);

    // === Admin retrieval methods ===

    public virtual async Task<TranslationBundleDto> GetAdminTranslationAsync(
        string realm, string lang, CancellationToken ct = default)
    {
        var entity = await repository.GetByRealmAndLangAsync(realm, lang, ct)
            ?? throw ServiceException.Of(TranslationServiceErrors.TranslationNotFound);
        return MapToDto(entity);
    }

    public virtual async Task<PagedResult<RealmLanguagesDto>> ListRealmsAsync(
        QueryParameters parameters, CancellationToken ct = default)
    {
        var allBundles = await repository.GetPagedAsync(parameters, ct);

        var grouped = allBundles.Items
            .GroupBy(b => b.Realm)
            .Select(g => new RealmLanguagesDto(
                g.Key,
                g.Select(b => new LanguageInfoDto(b.Lang, b.UpdatedAt, b.UpdatedBy))
                    .OrderBy(l => l.Lang)
                    .ToList()
            ))
            .ToList();

        return new PagedResult<RealmLanguagesDto>(
            grouped,
            allBundles.TotalElements,
            allBundles.Page,
            allBundles.PageSize
        );
    }

    // === Admin write methods ===

    public virtual async Task<TranslationBundleDto> CreateTranslationAsync(
        string realm, string lang, TranslationRequest request, CancellationToken ct = default)
    {
        if (await repository.ExistsByRealmAndLangAsync(realm, lang, ct))
            throw ServiceException.Of(TranslationServiceErrors.TranslationAlreadyExists);

        var entity = new TranslationBundleEntity
        {
            Realm = realm,
            Lang = lang,
            Data = request.Translations,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = currentUserService.GetCurrentUserUuid()
        };

        repository.Add(entity);
        return MapToDto(entity);
    }

    public virtual async Task<TranslationBundleDto> UpdateTranslationAsync(
        string realm, string lang, TranslationRequest request, CancellationToken ct = default)
    {
        var entity = await repository.GetByRealmAndLangAsync(realm, lang, ct)
            ?? throw ServiceException.Of(TranslationServiceErrors.TranslationNotFound);

        entity.Data = request.Translations;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = currentUserService.GetCurrentUserUuid();

        repository.Update(entity);
        return MapToDto(entity);
    }

    public virtual async Task DeleteTranslationAsync(
        string realm, string lang, CancellationToken ct = default)
    {
        var entity = await repository.GetByRealmAndLangAsync(realm, lang, ct)
            ?? throw ServiceException.Of(TranslationServiceErrors.TranslationNotFound);

        repository.Remove(entity);
    }

    private static TranslationBundleDto MapToDto(TranslationBundleEntity entity) => new(
        entity.Id,
        entity.Realm,
        entity.Lang,
        Translations: entity.Data,
        entity.UpdatedAt,
        entity.UpdatedBy
    );
}
