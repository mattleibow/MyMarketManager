using Microsoft.EntityFrameworkCore;
using MyMarketManager.Data;

namespace MyMarketManager.Tests.Shared;

/// <summary>
/// Base class for integration tests using a real PostgreSQL instance with pgvector in Docker.
/// Requires Docker to be running on the machine.
/// </summary>
public abstract class PostgresTestBase(ITestOutputHelper outputHelper, bool createSchema) : IAsyncLifetime
{
    private readonly PostgresHelper _postgres = new(outputHelper);

    protected MyMarketManagerDbContext Context { get; private set; } = null!;

    protected CancellationToken Cancel => TestContext.Current.CancellationToken;

    public virtual async ValueTask InitializeAsync()
    {
        var connectionString = await _postgres.ConnectAsync();

        var options = new DbContextOptionsBuilder<MyMarketManagerDbContext>()
            .UseNpgsql(connectionString, o => o.UseVector())
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

        await _postgres.DisconnectAsync();
    }

    protected async Task<bool> TableExistsAsync(string tableName)
    {
        try
        {
            var sql = """
                SELECT COUNT(*)
                FROM information_schema.tables
                WHERE table_name = @p0
                """;

            var count = await Context.Database
                .SqlQueryRaw<int>(sql, tableName.ToLower())
                .FirstOrDefaultAsync(Cancel);

            return count > 0;
        }
        catch
        {
            return false;
        }
    }
}
