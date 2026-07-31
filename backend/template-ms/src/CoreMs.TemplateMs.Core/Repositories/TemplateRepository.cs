using CoreMs.Common.Repository;
using CoreMs.Common.Extensions;
using CoreMs.TemplateMs.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoreMs.TemplateMs.Core.Repositories;

[Repository]
public class TemplateRepository(DbContext context) : SearchableRepository<TemplateEntity>(context)
{
    protected override IReadOnlySet<string> SearchFields => new HashSet<string> { "Name", "Description", "TemplateId" };
    protected override IReadOnlySet<string> SortFields => new HashSet<string> { "CreatedAt", "Name", "TemplateId", "Category" };
    protected override IReadOnlySet<string> FilterFields => new HashSet<string> { "Category", "Language" };

    protected override IQueryable<TemplateEntity> BaseQuery() => DbSet.Where(t => !t.IsDeleted);

    public virtual async Task<TemplateEntity?> GetByUuidAsync(Guid uuid, CancellationToken ct = default)
        => await BaseQuery().FirstOrDefaultAsync(t => t.Uuid == uuid, ct);

    public virtual async Task<TemplateEntity?> GetByTemplateIdAndLanguageAsync(string templateId, string language, CancellationToken ct = default)
        => await BaseQuery().FirstOrDefaultAsync(t => t.TemplateId == templateId && t.Language == language, ct);

    public virtual async Task<bool> ExistsByTemplateIdAndLanguageAsync(string templateId, string language, CancellationToken ct = default)
        => await BaseQuery().AnyAsync(t => t.TemplateId == templateId && t.Language == language, ct);
}
