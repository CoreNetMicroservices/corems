using CoreMs.Common.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreMs.CommunicationMs.Infrastructure.Data;

public class CommunicationMsDbContext(DbContextOptions<CommunicationMsDbContext> options) : CoreMsDbContext(options)
{
    protected override string SchemaName => "communication_ms";
}
