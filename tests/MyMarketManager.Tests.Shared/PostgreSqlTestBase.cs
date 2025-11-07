using Microsoft.EntityFrameworkCore;
using MyMarketManager.Data;
using Pgvector.EntityFrameworkCore;

namespace MyMarketManager.Tests.Shared;

/// <summary>
/// Base class for integration tests using a real PostgreSQL instance with pgvector in Docker.
/// Requires Docker to be running on the machine.
/// </summary>
public abstract class PostgreSqlTestBase(ITestOutputHelper outputHelper, bool createSchema) : IAsyncLifetime
{
    private readonly PostgreSqlHelper _postgreSql = new(outputHelper);

    protected MyMarketManagerDbContext Context { get; private set; } = null!;

    protected CancellationToken Cancel => TestContext.Current.CancellationToken;

    public virtual async ValueTask InitializeAsync()
    {
        var connectionString = await _postgreSql.ConnectAsync();

        var options = new DbContextOptionsBuilder<MyMarketManagerDbContext>()
            .UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.UseVector();
            })
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

        await _postgreSql.DisconnectAsync();
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
