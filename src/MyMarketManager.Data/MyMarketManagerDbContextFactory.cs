using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace MyMarketManager.Data;

public class MyMarketManagerDbContextFactory : IDesignTimeDbContextFactory<MyMarketManagerDbContext>
{
    public MyMarketManagerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MyMarketManagerDbContext>()
            .UseNpgsql("Host=localhost;Database=dummy;Username=dummy;Password=dummy");

        return new MyMarketManagerDbContext(optionsBuilder.Options);
    }
}
