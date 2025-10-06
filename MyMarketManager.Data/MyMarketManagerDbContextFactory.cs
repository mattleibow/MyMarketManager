using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MyMarketManager.Data;

/// <summary>
/// Design-time factory for creating DbContext instances for EF Core tools (migrations).
/// This is only used by EF Core tooling and not in the actual application.
/// </summary>
public class MyMarketManagerDbContextFactory : IDesignTimeDbContextFactory<MyMarketManagerDbContext>
{
    public MyMarketManagerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MyMarketManagerDbContext>();
        
        // This connection string is only used for design-time (migrations)
        // The actual application should provide its own connection string at runtime
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=MyMarketManager;Trusted_Connection=True;MultipleActiveResultSets=true");

        return new MyMarketManagerDbContext(optionsBuilder.Options);
    }
}
