using MyMarketManager.Data;
using MyMarketManager.Data.Services;
using MyMarketManager.WebApp.Components;
using MyMarketManager.WebApp.GraphQL;
using MyMarketManager.WebApp.Services;
using MyMarketManager.GraphQL.Client;
using MyMarketManager.Scrapers;
using MyMarketManager.Scrapers.Shein;
using MyMarketManager.Data.Processing;

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

// Add Azure AI Vision service for image vectorization
var visionEndpoint = builder.Configuration["AzureVision:Endpoint"] ?? "";
var visionApiKey = builder.Configuration["AzureVision:ApiKey"] ?? "";
builder.Services.AddHttpClient<IAzureVisionService, AzureVisionService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(2);
})
.ConfigureHttpClient((sp, client) =>
{
    // HttpClient is already injected, no additional configuration needed
});
builder.Services.AddScoped<IAzureVisionService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(AzureVisionService));
    var logger = sp.GetRequiredService<ILogger<AzureVisionService>>();
    return new AzureVisionService(httpClient, logger, visionEndpoint, visionApiKey);
});

// Add image vectorization services
builder.Services.AddScoped<ImageVectorizationProcessor>();
builder.Services.AddHostedService<ImageVectorizationBackgroundService>();

// Add scraper services
builder.Services.Configure<ScraperConfiguration>(builder.Configuration.GetSection("Scraper"));
builder.Services.AddScoped<IWebScraperSessionFactory, WebScraperSessionFactory>();

// Add ingestion services
builder.Services.Configure<IngestionServiceOptions>(builder.Configuration.GetSection("IngestionService"));
builder.Services.AddScoped<BatchProcessingService>();
builder.Services.AddHostedService<IngestionService>();
builder.Services.AddBatchProcessorFactory()
    .AddWebScraper<SheinWebScraper>("Shein");

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Add GraphQL server with HotChocolate first
builder.Services
    .AddGraphQLServer()
    .AddQueryType(d => d.Name("Query"))
        .AddTypeExtension<ProductQueries>()
        .AddTypeExtension<PurchaseOrderQueries>()
        .AddTypeExtension<PurchaseOrderIngestionQueries>()
        .AddTypeExtension<StagingBatchQueries>()
        .AddTypeExtension<SupplierQueries>()
    .AddMutationType(d => d.Name("Mutation"))
        .AddTypeExtension<ProductMutations>()
        .AddTypeExtension<PurchaseOrderIngestionMutations>();

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
