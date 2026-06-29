using CoreMs.Common.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreMs.DocumentMs.Infrastructure.Data;

public class DocumentMsDbContext(DbContextOptions<DocumentMsDbContext> options) : CoreMsDbContext(options)
{
    protected override string SchemaName => "document_ms";
}
