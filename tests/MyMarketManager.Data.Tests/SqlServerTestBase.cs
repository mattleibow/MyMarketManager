using Microsoft.EntityFrameworkCore;
using MyMarketManager.Tests.Shared;

namespace MyMarketManager.Data.Tests;

/// <summary>
/// Base class for integration tests using a real SQL Server instance in Docker.
/// Requires Docker to be running on the machine.
/// </summary>
public abstract class SqlServerTestBase(ITestOutputHelper outputHelper, bool createSchema) : IAsyncLifetime
{
    private readonly SqlServerHelper _sqlServer = new(outputHelper);

    protected MyMarketManagerDbContext Context { get; private set; } = null!;

    protected CancellationToken Cancel => TestContext.Current.CancellationToken;

    public virtual async ValueTask InitializeAsync()
    {
        var connectionString = await _sqlServer.ConnectAsync();

        var options = new DbContextOptionsBuilder<MyMarketManagerDbContext>()
            .UseSqlServer(connectionString)
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

        await _sqlServer.DisconnectAsync();
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
