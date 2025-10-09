using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// Check if a connection string is provided (e.g., from tests)
var externalConnectionString = builder.Configuration.GetConnectionString("database");

IResourceBuilder<IResourceWithConnectionString> database;

if (!string.IsNullOrEmpty(externalConnectionString))
{
    // Use the provided connection string (from Testcontainers in tests)
    database = builder.AddConnectionString("database", externalConnectionString);
}
else
{
    // Create SQL Server container for normal operation
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
    .WaitFor(database);

builder.Build().Run();
