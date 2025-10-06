using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MyMarketManager.Data.Tests;

/// <summary>
/// Base class for tests that use SQLite in-memory database.
/// Based on https://learn.microsoft.com/en-us/ef/core/testing/testing-without-the-database#sqlite-in-memory
/// </summary>
public abstract class SqliteTestBase : IDisposable
{
    private readonly SqliteConnection _connection;
    protected readonly MyMarketManagerDbContext Context;

    protected SqliteTestBase()
    {
        // SQLite in-memory database only exists while the connection is open
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<MyMarketManagerDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new MyMarketManagerDbContext(options);
        
        // Create the schema
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context?.Dispose();
        _connection?.Dispose();
    }
}
