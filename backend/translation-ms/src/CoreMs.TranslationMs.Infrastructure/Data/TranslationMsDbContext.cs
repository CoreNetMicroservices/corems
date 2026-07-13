using CoreMs.Common.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreMs.TranslationMs.Infrastructure.Data;

public class TranslationMsDbContext(DbContextOptions<TranslationMsDbContext> options) : CoreMsDbContext(options)
{
    protected override string SchemaName => "translation_ms";
}
