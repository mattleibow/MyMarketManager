using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MyMarketManager.Data.Tests;

/// <summary>
/// Base class for tests using SQLite in-memory database.
/// SQLite in-memory databases only exist while the connection is open,
/// so we need to keep the connection open for the lifetime of the test class.
/// </summary>
public abstract class SqliteTestBase(bool createSchema = true) : IAsyncLifetime
{
    protected SqliteConnection Connection { get; private set; } = null!;

    protected MyMarketManagerDbContext Context { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        // SQLite in-memory database only exists while the connection is open
        Connection = new SqliteConnection("DataSource=:memory:");
        Connection.Open();

        var options = new DbContextOptionsBuilder<MyMarketManagerDbContext>()
            .UseSqlite(Connection)
            .Options;

        Context = new MyMarketManagerDbContext(options);

        // Create the schema
        if (createSchema)
        {
            await Context.Database.EnsureCreatedAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();

        await Connection.DisposeAsync();
    }
}
