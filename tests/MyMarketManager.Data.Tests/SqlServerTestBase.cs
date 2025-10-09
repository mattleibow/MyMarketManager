using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace MyMarketManager.Data.Tests;

/// <summary>
/// Base class for integration tests using a real SQL Server instance in Docker.
/// Requires Docker to be running on the machine.
/// </summary>
public abstract class SqlServerTestBase(bool createSchema) : IAsyncLifetime
{
    protected MsSqlContainer SqlContainer { get; private set; } = null!;

    protected MyMarketManagerDbContext Context { get; private set; } = null!;

    protected CancellationToken Cancel => TestContext.Current.CancellationToken;

    public virtual async ValueTask InitializeAsync()
    {
        // Create SQL Server container
        SqlContainer = new MsSqlBuilder()
            .Build();

        // Start container and create context
        await SqlContainer.StartAsync();

        var options = new DbContextOptionsBuilder<MyMarketManagerDbContext>()
            .UseSqlServer(SqlContainer.GetConnectionString())
            .Options;

        Context = new MyMarketManagerDbContext(options);

        // Create the database schema
        if (createSchema)
        {
            await Context.Database.EnsureCreatedAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();

        await SqlContainer.DisposeAsync();
    }

    protected async Task<bool> TableExistsAsync(string tableName)
    {
        try
        {
            var sql = """
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_NAME = @tableName
                """;

            var count = await Context.Database
                .SqlQueryRaw<int>(sql, tableName)
                .FirstOrDefaultAsync(Cancel);

            return count > 0;
        }
        catch
        {
            return false;
        }
    }
}
