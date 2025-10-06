using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MyMarketManager.Data;

/// <summary>
/// Design-time factory for creating DbContext instances for EF Core tooling (e.g., migrations).
/// </summary>
public class MyMarketManagerDbContextFactory : IDesignTimeDbContextFactory<MyMarketManagerDbContext>
{
    public MyMarketManagerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MyMarketManagerDbContext>();
        
        // This is a placeholder connection string for design-time only.
        // In production, the connection string should come from configuration.
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=MyMarketManager;Trusted_Connection=True;MultipleActiveResultSets=true");

        return new MyMarketManagerDbContext(optionsBuilder.Options);
    }
}
