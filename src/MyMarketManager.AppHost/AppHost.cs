var builder = DistributedApplication.CreateBuilder(args);

// Check if we should use SQLite for testing
bool useSqlite = false;
bool.TryParse(builder.Configuration["UseSqlite"], out useSqlite);

if (useSqlite)
{
    // For testing: use SQLite (no external dependencies)
    var sqlite = builder.AddSqlite("mymarketmanager")
        .WithSqliteWeb();
    
    builder.AddProject<Projects.MyMarketManager_WebApp>("webapp")
        .WithReference(sqlite);
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
