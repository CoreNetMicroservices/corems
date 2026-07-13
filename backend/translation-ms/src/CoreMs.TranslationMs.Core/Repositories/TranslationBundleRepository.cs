using CoreMs.Common.Extensions;
using CoreMs.Common.Repository;
using CoreMs.TranslationMs.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoreMs.TranslationMs.Core.Repositories;

[Repository]
public class TranslationBundleRepository(DbContext context) : SearchableRepository<TranslationBundleEntity>(context)
{
    protected override IReadOnlySet<string> SearchFields => new HashSet<string> { "Realm", "Lang" };
    protected override IReadOnlySet<string> SortFields => new HashSet<string> { "UpdatedAt", "Realm", "Lang" };
    protected override IReadOnlySet<string> FilterFields => new HashSet<string> { "Realm", "Lang", "UpdatedBy" };

    public virtual async Task<TranslationBundleEntity?> GetByRealmAndLangAsync(
        string realm, string lang, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(t => t.Realm == realm && t.Lang == lang, ct);

    public virtual async Task<List<string>> GetLanguagesByRealmAsync(
        string realm, CancellationToken ct = default)
        => await DbSet
            .Where(t => t.Realm == realm)
            .Select(t => t.Lang)
            .Distinct()
            .OrderBy(l => l)
            .ToListAsync(ct);

    public virtual async Task<bool> ExistsByRealmAndLangAsync(
        string realm, string lang, CancellationToken ct = default)
        => await DbSet.AnyAsync(t => t.Realm == realm && t.Lang == lang, ct);
}
