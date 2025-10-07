var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddAzureSqlServer("sql");

var database = sqlServer.AddDatabase("mymarketmanager");

builder.AddProject<Projects.MyMarketManager_WebApp>("webapp")
    .WithReference(database);

builder.Build().Run();
