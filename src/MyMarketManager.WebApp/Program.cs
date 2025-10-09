using MyMarketManager.Data;
using MyMarketManager.Data.Services;
using MyMarketManager.WebApp.Components;
using MyMarketManager.WebApp.GraphQL;
using MyMarketManager.WebApp.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (health checks, service discovery, and telemetry)
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Check if we should use SQLite (primarily for tests)
var useSqlite = builder.Configuration.GetValue("UseSqliteDatabase", false);

if (useSqlite)
{
    // Use SQLite in-memory database for tests
    // Note: For in-memory SQLite to work across requests, we need to use a shared cache connection
    var connectionString = builder.Configuration.GetConnectionString("database") ?? "Data Source=:memory:";
    
    builder.Services.AddDbContext<MyMarketManagerDbContext>(options =>
        options.UseSqlite(connectionString));
}
else
{
    // Configure DbContext to use the connection string provided by Aspire
    builder.AddSqlServerDbContext<MyMarketManagerDbContext>("database");
}

// Add database migration as a hosted service (runs in all environments)
builder.Services.AddScoped<MyMarketManagerDbContextMigrator>();
builder.Services.AddHostedService<DatabaseMigrationService>();

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
