using MyMarketManager.Data;
using MyMarketManager.Data.Services;
using MyMarketManager.Data.Enums;
using MyMarketManager.WebApp.Components;
using MyMarketManager.WebApp.GraphQL;
using MyMarketManager.WebApp.Services;
using MyMarketManager.GraphQL.Client;
using MyMarketManager.Scrapers;
using MyMarketManager.Scrapers.Shein;
using HotChocolate.Execution;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (health checks, service discovery, and telemetry)
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure DbContext to use the connection string provided by Aspire
builder.AddSqlServerDbContext<MyMarketManagerDbContext>("database");

// Add database migration as a hosted service (runs in all environments)
builder.Services.AddScoped<DbContextMigrator>();
builder.Services.AddHostedService<DatabaseMigrationService>();

// Add web scraper services
builder.Services.Configure<ScraperConfiguration>(builder.Configuration.GetSection("Scraper"));
builder.Services.AddScoped<IWebScraperSessionFactory, WebScraperSessionFactory>();

// Register web scrapers
builder.Services.AddScoped<SheinWebScraper>();
// Future scrapers can be registered here:
// builder.Services.AddScoped<AnotherWebScraper>();

// Register batch processor factory and configure it
builder.Services.AddSingleton<BatchProcessorFactory>(sp =>
{
    var factory = new BatchProcessorFactory(sp);
    
    // Register web scrapers
    factory.Register<SheinWebScraper>(MyMarketManager.Data.Enums.StagingBatchType.WebScrape, "Shein");
    // Future scrapers:
    // factory.Register<AnotherWebScraper>(StagingBatchType.WebScrape, "AnotherSupplier");
    
    // Future batch types can be registered here:
    // factory.Register<SalesDataProcessor>(StagingBatchType.SalesData, "YocoApi");
    
    return factory;
});

// Add ingestion background service
builder.Services.AddHostedService<IngestionService>();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Add GraphQL server with HotChocolate first
builder.Services
    .AddGraphQLServer()
    .AddQueryType(d => d.Name("Query"))
        .AddTypeExtension<ProductQueries>()
        .AddTypeExtension<PoIngestionQueries>()
    .AddMutationType(d => d.Name("Mutation"))
        .AddTypeExtension<ProductMutations>()
        .AddTypeExtension<PoIngestionMutations>();

// Add GraphQL client using InMemory transport for server-side execution
// This avoids HTTP overhead and URL configuration issues
builder.Services
    .AddMyMarketManagerClient(profile: MyMarketManagerClientProfileKind.InMemory)
    .ConfigureInMemoryClient();

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
