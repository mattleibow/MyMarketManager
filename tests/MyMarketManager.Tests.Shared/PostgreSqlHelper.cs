using Testcontainers.PostgreSql;

namespace MyMarketManager.Tests.Shared;

public class PostgreSqlHelper(ITestOutputHelper outputHelper)
{
    private PostgreSqlContainer? _postgresContainer;
    private string? _connectionString;

    public async Task<string> ConnectAsync()
    {
        if (_connectionString is not null)
        {
            outputHelper.WriteLine("Reusing existing connection string: {0}", _connectionString);

            return _connectionString;
        }

        var databaseName = $"testdb_{Guid.NewGuid():N}";

        outputHelper.WriteLine("Using Testcontainers PostgreSQL with pgvector.");

        // Use Testcontainers with pgvector support
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("pgvector/pgvector:pg17")
            .WithDatabase(databaseName)
            .Build();

        await _postgresContainer.StartAsync(TestContext.Current.CancellationToken);
        _connectionString = _postgresContainer.GetConnectionString();

        outputHelper.WriteLine("Connection string: {0}", _connectionString);

        return _connectionString;
    }

    public virtual async Task DisconnectAsync()
    {
        if (_postgresContainer is not null)
        {
            outputHelper.WriteLine("Disposing PostgreSQL container.");

            await _postgresContainer.DisposeAsync();
        }

        _postgresContainer = null;
        _connectionString = null;
    }
}
