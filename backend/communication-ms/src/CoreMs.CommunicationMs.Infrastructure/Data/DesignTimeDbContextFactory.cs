using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CoreMs.CommunicationMs.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CommunicationMsDbContext>
{
    public CommunicationMsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CommunicationMsDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=corems;Username=postgres;Password=postgres;Search Path=communication_ms");
        return new CommunicationMsDbContext(optionsBuilder.Options);
    }
}
