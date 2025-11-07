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
    // Create PostgreSQL container with pgvector extension for normal operation
    var postgres = builder.AddPostgres("postgres")
        .WithImage("pgvector/pgvector")
        .WithImageTag("pg17")
        .WithLifetime(ContainerLifetime.Persistent);

    if (builder.Configuration.GetValue("UseVolumes", true))
        postgres.WithDataVolume();

    database = postgres.AddDatabase("database");
}

builder.AddProject<Projects.MyMarketManager_WebApp>("webapp")
    .WithReference(database)
    .WaitFor(database);

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
