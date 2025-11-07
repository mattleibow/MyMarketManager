using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pgvector.EntityFrameworkCore;

namespace MyMarketManager.Data;

public class MyMarketManagerDbContextFactory : IDesignTimeDbContextFactory<MyMarketManagerDbContext>
{
    public MyMarketManagerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MyMarketManagerDbContext>()
            .UseNpgsql("Host=localhost;Database=dummy;Username=postgres;Password=postgres", o => o.UseVector());

        return new MyMarketManagerDbContext(optionsBuilder.Options);
    }
}
