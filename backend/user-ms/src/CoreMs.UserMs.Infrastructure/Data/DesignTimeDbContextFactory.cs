using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CoreMs.UserMs.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<UserMsDbContext>
{
    public UserMsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UserMsDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=corems;Username=postgres;Password=postgres;Search Path=user_ms");
        return new UserMsDbContext(optionsBuilder.Options);
    }
}
