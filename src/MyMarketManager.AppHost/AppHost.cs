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

// Add Azure AI Foundry for image and text embeddings (optional)
// Control via UseAzureAIFoundry configuration setting (default: true in production, false in development)
var useAzureAIFoundry = builder.Configuration.GetValue("UseAzureAIFoundry", true);
IResourceBuilder<IResourceWithConnectionString>? aiFoundry = null;
if (useAzureAIFoundry)
{
    aiFoundry = builder.AddAzureAIFoundry("ai-foundry");
}

var webApp = builder.AddProject<Projects.MyMarketManager_WebApp>("webapp")
    .WithReference(database)
    .WaitFor(database);

if (aiFoundry != null)
{
    webApp.WithReference(aiFoundry);
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
}
