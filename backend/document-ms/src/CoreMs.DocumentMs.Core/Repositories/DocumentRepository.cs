using CoreMs.Common.Extensions;
using CoreMs.Common.Repository;
using CoreMs.DocumentMs.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoreMs.DocumentMs.Core.Repositories;

[Repository]
public class DocumentRepository(DbContext context) : SearchableRepository<DocumentEntity>(context)
{
    protected override IReadOnlySet<string> SearchFields => new HashSet<string> { "Name", "OriginalFilename", "Description" };
    protected override IReadOnlySet<string> SortFields => new HashSet<string> { "Name", "CreatedAt", "UpdatedAt", "Size" };
    protected override IReadOnlySet<string> FilterFields => new HashSet<string> { "Visibility", "Extension", "UserId", "IsDeleted" };

    protected override IQueryable<DocumentEntity> BaseQuery() => DbSet.Where(d => !d.IsDeleted);

    public virtual async Task<DocumentEntity?> GetByUuidAsync(Guid uuid, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(d => d.Uuid == uuid && !d.IsDeleted, ct);

    public virtual async Task<DocumentEntity?> GetByUuidIncludeDeletedAsync(Guid uuid, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(d => d.Uuid == uuid, ct);

    public virtual async Task<DocumentEntity?> GetByUserIdAndFilenameAsync(Guid userId, string filename, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(d => d.UserId == userId && d.OriginalFilename == filename && !d.IsDeleted, ct);

    public virtual async Task<List<DocumentEntity>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await DbSet.Where(d => d.UserId == userId && !d.IsDeleted).ToListAsync(ct);
}
