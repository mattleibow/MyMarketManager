using MyMarketManager.Data;
using MyMarketManager.Data.Services;
using MyMarketManager.WebApp.Components;
using MyMarketManager.WebApp.GraphQL;
using MyMarketManager.WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (health checks, service discovery, and telemetry)
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure DbContext to use the connection string provided by Aspire
builder.AddSqlServerDbContext<MyMarketManagerDbContext>("database");

// Configure Azure Blob Storage client provided by Aspire
builder.AddAzureBlobServiceClient("blobs");

// Add database migration as a hosted service (runs in all environments)
builder.Services.AddScoped<MyMarketManagerDbContextMigrator>();
builder.Services.AddHostedService<DatabaseMigrationService>();

// Add blob storage services
builder.Services.AddSingleton<MyMarketManager.Data.Services.BlobStorageService, AzureBlobStorageService>();
builder.Services.AddScoped<BatchIngestionProcessor>();
builder.Services.AddHostedService<BlobIngestionService>();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Add the GraphQL client to be used by the web app to call the GraphQL API
builder.Services.AddMyMarketManagerClient();

// Add GraphQL server with HotChocolate
builder.Services
    .AddGraphQLServer()
    .AddQueryType<ProductQueries>()
    .AddMutationType<ProductMutations>();

var app = builder.Build();

// Map Aspire default endpoints (health checks)
app.MapDefaultEndpoints();

// Map GraphQL endpoint
app.MapGraphQL();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
