using CoreMs.Common.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreMs.TemplateMs.Infrastructure.Data;

public class TemplateMsDbContext(DbContextOptions<TemplateMsDbContext> options) : CoreMsDbContext(options)
{
    protected override string SchemaName => "template_ms";
}
