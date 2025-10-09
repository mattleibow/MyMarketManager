using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// Check if there's an override connection string for testing
var connectionStringOverride = builder.Configuration.GetValue<string>("DatabaseConnectionString");

if (!string.IsNullOrEmpty(connectionStringOverride))
{
    // Use external SQL Server (for testing with testcontainers)
    // Create a parameter resource for the connection string
    var connectionString = builder.AddParameter("database-connection-string", connectionStringOverride, secret: false);
    
    // Add the webapp with the connection string parameter
    builder.AddProject<Projects.MyMarketManager_WebApp>("webapp")
        .WithEnvironment("ConnectionStrings__database", connectionString);
}
else
{
    // Use containerized SQL Server (normal operation)
    var sqlServer = builder.AddAzureSqlServer("sql")
        .RunAsContainer(container =>
        {
            container.WithImageTag("2022-latest");

            if (builder.Configuration.GetValue("UseVolumes", true))
                container.WithDataVolume();
        });

    var database = sqlServer.AddDatabase("database");

    builder.AddProject<Projects.MyMarketManager_WebApp>("webapp")
        .WithReference(database)
        .WaitFor(database);
}

builder.Build().Run();
