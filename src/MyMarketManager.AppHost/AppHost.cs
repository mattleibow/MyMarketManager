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

var blobStorage = builder.AddAzureStorage("storage")
    .RunAsEmulator(emulator =>
    {
        emulator.WithImageTag("latest");

        if (builder.Configuration.GetValue("UseVolumes", true))
            emulator.WithDataVolume();
    });

var blobs = blobStorage.AddBlobs("blobs");

builder.AddProject<Projects.MyMarketManager_WebApp>("webapp")
    .WithReference(database)
    .WithReference(blobs)
    .WaitFor(database)
    .WaitFor(blobs);

builder.Build().Run();
