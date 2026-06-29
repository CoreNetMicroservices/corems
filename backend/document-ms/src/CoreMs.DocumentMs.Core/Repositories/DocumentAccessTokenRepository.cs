using CoreMs.Common.Extensions;
using CoreMs.Common.Repository;
using CoreMs.DocumentMs.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoreMs.DocumentMs.Core.Repositories;

[Repository]
public class DocumentAccessTokenRepository(DbContext context) : CrudRepository<DocumentAccessTokenEntity>(context)
{
    public virtual async Task<DocumentAccessTokenEntity?> GetByTokenHashAndDocumentAsync(string tokenHash, Guid documentUuid, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.DocumentUuid == documentUuid, ct);

    public virtual async Task<List<DocumentAccessTokenEntity>> GetByDocumentUuidAsync(Guid documentUuid, CancellationToken ct = default)
        => await DbSet.Where(t => t.DocumentUuid == documentUuid).ToListAsync(ct);
}
