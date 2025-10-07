using MyMarketManager.Data;
using MyMarketManager.WebApp.Components;
using MyMarketManager.WebApp.Services;
using MyMarketManager.ApiClient.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (health checks, service discovery, and telemetry)
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add API controllers
builder.Services.AddControllers();

// Add Entity Framework with Aspire
builder.AddSqlServerDbContext<MyMarketManagerDbContext>("mymarketmanager");

// Add database migration as a hosted service (runs in all environments)
builder.Services.AddHostedService<DatabaseMigrationService>();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configure HttpClient for ProductsClient to use the local API
builder.Services.AddProductsClient(client =>
{
    // Since the API is hosted in the same app, we'll use a relative base address
    // The HttpClient will automatically use the current host
});

var app = builder.Build();

// Map Aspire default endpoints (health checks)
app.MapDefaultEndpoints();

// Map API controllers
app.MapControllers();

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
