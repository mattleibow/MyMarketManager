var builder = DistributedApplication.CreateBuilder(args);

// Check if we should use SQLite for testing
bool useSqlite = false;
bool.TryParse(builder.Configuration["UseSqlite"], out useSqlite);

if (useSqlite)
{
    // For testing: use SQLite (no external dependencies)
    builder.AddProject<Projects.MyMarketManager_WebApp>("webapp")
        .WithEnvironment("UseSqlite", "true")
        .WithEnvironment("ConnectionStrings__sqlite", "Data Source=mymarketmanager_test.db");
}
else
{
    // For production: use Azure SQL Server
    var sqlServer = builder.AddAzureSqlServer("sql");
    var database = sqlServer.AddDatabase("mymarketmanager");
    
    builder.AddProject<Projects.MyMarketManager_WebApp>("webapp")
        .WithReference(database);
}

builder.Build().Run();
