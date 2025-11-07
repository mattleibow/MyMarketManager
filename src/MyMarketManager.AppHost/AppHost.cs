using Aspire.Hosting.Azure;
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
    // Create PostgreSQL container for normal operation
    var postgres = builder.AddPostgres("postgres")
        .WithImage("pgvector/pgvector")
        .WithImageTag("pg17")
        .WithLifetime(ContainerLifetime.Persistent);

    if (builder.Configuration.GetValue("UseVolumes", true))
        postgres.WithDataVolume();

    database = postgres.AddDatabase("database");
}

var webApp = builder.AddProject<Projects.MyMarketManager_WebApp>("webapp")
    .WithReference(database)
    .WaitFor(database);

if (builder.GetDevConfig("UseAzureAIFoundry", true))
{
    var ai = builder.AddAzureAIFoundry("ai-foundry");

    var embedding = ai.AddDeployment("ai-embedding", AIFoundryModel.Cohere.CohereEmbedV3English);

    webApp.WithReference(embedding);
}

builder.Build().Run();


static class Extensions
{
    public static string? GetDevConfig(this IDistributedApplicationBuilder builder, string key) =>
        builder.ExecutionContext.IsPublishMode ||
        builder.Configuration.GetValue(key, "") is not { } value ||
        string.IsNullOrEmpty(value)
            ? null
            : value;

    public static bool GetDevConfig(this IDistributedApplicationBuilder builder, string key, bool defaultValue) =>
        builder.ExecutionContext.IsPublishMode
            ? defaultValue
            : builder.Configuration.GetValue(key, defaultValue);
}
