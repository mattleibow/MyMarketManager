var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddAzureSqlServer("sql");

var database = sqlServer.AddDatabase("mymarketmanager");

builder.Build().Run();
