using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MyMarketManager.Data;

public class MyMarketManagerDbContextFactory : IDesignTimeDbContextFactory<MyMarketManagerDbContext>
{
    public MyMarketManagerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MyMarketManagerDbContext>()
            .UseSqlServer("Data Source=dummy");

        return new MyMarketManagerDbContext(optionsBuilder.Options);
    }
}
