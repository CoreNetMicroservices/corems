using CoreMs.Common.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreMs.UserMs.Infrastructure.Data;

public class UserMsDbContext(DbContextOptions<UserMsDbContext> options) : CoreMsDbContext(options)
{
    protected override string SchemaName => "user_ms";
}
