using Microsoft.Data.Sqlite;

namespace MyMarketManager.Tests.Shared;

public class SqliteHelper(ITestOutputHelper outputHelper)
{
    private SqliteConnection? _connection;

    public async Task<SqliteConnection> ConnectAsync()
    {
        if (_connection is not null)
        {
            outputHelper.WriteLine("Reusing existing SQLite connection.");

            return _connection;
        }

        // SQLite in-memory database only exists while the connection is open
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        outputHelper.WriteLine("Opened new SQLite in-memory connection.");

        return _connection;
    }

    public async ValueTask DisconnectAsync()
    {
        if (_connection is not null)
        {
            outputHelper.WriteLine("Disposing SQLite connection.");

            await _connection.DisposeAsync();
        }

        _connection = null;
    }
}
