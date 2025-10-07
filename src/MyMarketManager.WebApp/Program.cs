using MyMarketManager.Data;
using MyMarketManager.WebApp.Components;
using MyMarketManager.WebApp.Services;
using MyMarketManager.WebApp.GraphQL;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (health checks, service discovery, and telemetry)
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure database based on environment or configuration
var useSqlite = builder.Configuration.GetValue<bool>("UseSqlite");
if (useSqlite)
{
    // Use SQLite for development/testing
    var sqliteConnectionString = builder.Configuration.GetConnectionString("sqlite") 
        ?? "Data Source=mymarketmanager.db";
    builder.Services.AddDbContext<MyMarketManagerDbContext>(options =>
        options.UseSqlite(sqliteConnectionString));
}
else
{
    // Use SQL Server with Aspire
    builder.AddSqlServerDbContext<MyMarketManagerDbContext>("mymarketmanager");
}

// Add database migration as a hosted service (runs in all environments)
builder.Services.AddHostedService<DatabaseMigrationService>();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

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

// Make the implicit Program class public for testing
public partial class Program { }
