using CoreMs.Common.Extensions;
using CoreMs.Common.Repository;
using CoreMs.CommunicationMs.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoreMs.CommunicationMs.Core.Repositories;

[Repository]
public class MessageRepository(DbContext context) : SearchableRepository<MessageEntity>(context)
{
    protected override IReadOnlySet<string> SearchFields => new HashSet<string>();
    protected override IReadOnlySet<string> SortFields => new HashSet<string> { "CreatedAt" };
    protected override IReadOnlySet<string> FilterFields => new HashSet<string> { "UserId", "Type", "Status", "SentByType", "SentById" };

    public virtual async Task<MessageEntity?> GetByUuidAsync(Guid uuid, CancellationToken ct = default)
        => await DbSet
            .Include(m => (m as EmailMessageEntity)!.Attachments)
            .FirstOrDefaultAsync(m => m.Uuid == uuid, ct);
}
