using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<IResourceWithConnectionString> database;

if (builder.GetDevConfig("UseDatabaseConnectionString") is { } connStr)
{
    // Use the provided connection string (only in development)
    database = builder.AddConnectionString("database", ReferenceExpression.Create($"{connStr}"));
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


static class Extensions
{
    public static string? GetDevConfig(this IDistributedApplicationBuilder builder, string key) =>
        builder.ExecutionContext.IsPublishMode ||
        builder.Configuration.GetValue(key, "") is not { } value ||
        string.IsNullOrEmpty(value)
            ? null
            : value;
}
