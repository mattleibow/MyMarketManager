using Microsoft.EntityFrameworkCore;
using MyMarketManager.Data;

namespace MyMarketManager.Tests.Shared;

/// <summary>
/// Base class for tests using SQLite in-memory database.
/// SQLite in-memory databases only exist while the connection is open,
/// so we need to keep the connection open for the lifetime of the test class.
/// </summary>
public abstract class SqliteTestBase(ITestOutputHelper outputHelper, bool createSchema = true) : IAsyncLifetime
{
    private readonly SqliteHelper _sqlite = new(outputHelper);

    protected MyMarketManagerDbContext Context { get; private set; } = null!;

    protected CancellationToken Cancel => TestContext.Current.CancellationToken;

    public virtual async ValueTask InitializeAsync()
    {
        var connection = await _sqlite.ConnectAsync();

        var options = new DbContextOptionsBuilder<MyMarketManagerDbContext>()
            .UseSqlite(connection)
            .Options;

        Context = new MyMarketManagerDbContext(options);

        // Create the schema
        if (createSchema)
        {
            await Context.Database.EnsureCreatedAsync();
        }
    }

    public virtual async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();

        await _sqlite.DisconnectAsync();
    }
}
