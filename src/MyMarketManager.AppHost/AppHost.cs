using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// Check if we should use SQLite (primarily for tests)
var useSqlite = builder.Configuration.GetValue("UseSqliteDatabase", false);

IResourceBuilder<IResourceWithConnectionString> database;

if (useSqlite)
{
    // Use SQLite file-based database for tests (in-memory would be closed between connections)
    var sqliteDbPath = Path.Combine(Path.GetTempPath(), $"mymarketmanager_test_{Guid.NewGuid()}.db");
    database = builder.AddConnectionString("database", $"Data Source={sqliteDbPath}");
}
else
{
    // Use SQL Server for normal operation
    var sqlServer = builder.AddAzureSqlServer("sql")
        .RunAsContainer(container =>
        {
            container.WithImageTag("2022-latest");
            container.WithLifetime(ContainerLifetime.Persistent);

            if (builder.Configuration.GetValue("UseVolumes", true))
                container.WithDataVolume();
        });

    database = sqlServer.AddDatabase("database");
}

builder.AddProject<Projects.MyMarketManager_WebApp>("webapp")
    .WithReference(database)
    .WithEnvironment("UseSqliteDatabase", useSqlite.ToString())
    .WaitFor(database);

builder.Build().Run();
