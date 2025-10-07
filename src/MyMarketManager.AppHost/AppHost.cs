using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

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

builder.Build().Run();
