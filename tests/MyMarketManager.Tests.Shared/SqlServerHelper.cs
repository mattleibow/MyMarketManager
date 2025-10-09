using Testcontainers.MsSql;

namespace MyMarketManager.Tests.Shared;

public class SqlServerHelper(ITestOutputHelper outputHelper)
{
    private MsSqlContainer? _sqlContainer;
    private string? _connectionString;

    public async Task<string> ConnectAsync()
    {
        if (_connectionString is not null)
        {
            outputHelper.WriteLine("Reusing existing connection string: {0}", _connectionString);

            return _connectionString;
        }

        var databaseName = $"TestDb_{Guid.NewGuid():N}";

        if (OperatingSystem.IsWindows())
        {
            outputHelper.WriteLine("Using SQL Server LocalDB on Windows.");

            // Use SQL Server LocalDB on Windows
            _connectionString = $"Server=(localdb)\\mssqllocaldb;Database={databaseName};Trusted_Connection=true;TrustServerCertificate=true;";
        }
        else
        {
            outputHelper.WriteLine("Using Testcontainers SQL Server on non-Windows platform.");

            // Use Testcontainers on non-Windows platforms
            _sqlContainer = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .Build();

            await _sqlContainer.StartAsync(TestContext.Current.CancellationToken);
            _connectionString = _sqlContainer.GetConnectionString();
        }

        outputHelper.WriteLine("Connection string: {0}", _connectionString);

        return _connectionString;
    }

    public virtual async Task DisconnectAsync()
    {
        if (_sqlContainer is not null)
        {
            outputHelper.WriteLine("Disposing SQL container.");

            await _sqlContainer.DisposeAsync();
        }

        _sqlContainer = null;
        _connectionString = null;
    }
}
