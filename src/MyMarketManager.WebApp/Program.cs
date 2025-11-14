using MyMarketManager.Data;
using MyMarketManager.Data.Services;
using MyMarketManager.WebApp.Components;
using MyMarketManager.WebApp.GraphQL;
using MyMarketManager.WebApp.GraphQL.Types;
using MyMarketManager.WebApp.Services;
using MyMarketManager.GraphQL.Client;
using MyMarketManager.Scrapers;
using MyMarketManager.Scrapers.Shein;
using MyMarketManager.Processing;
using MyMarketManager.Processing.Handlers;
using MyMarketManager.AI;
using Pgvector.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (health checks, service discovery, and telemetry)
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure DbContext to use the connection string provided by Aspire
builder.AddNpgsqlDbContext<MyMarketManagerDbContext>("database",
    configureDbContextOptions: dbContextOptions =>
    {
        dbContextOptions.UseNpgsql(npgsqlOptions =>
        {
            npgsqlOptions.UseVector();
        });
        // Use custom model cache key factory to support different database providers
        dbContextOptions.ReplaceService<IModelCacheKeyFactory, MyMarketManagerModelCacheKeyFactory>();
    });

// Add database migration as a hosted service (runs in all environments)
builder.Services.AddScoped<DbContextMigrator>();
builder.Services.AddHostedService<DatabaseMigrationService>();

// Add Azure AI Foundry embedding generators (if configured)
if (builder.Configuration.GetConnectionString("ai-embedding") is { } embeddingConnectionString)
{
    // Registers IEmbeddingGenerator<string, Embedding<float>> for text
    // and IEmbeddingGenerator<DataContent, Embedding<float>> for images
    builder.Services.AddAzureAIFoundryEmbeddings(embeddingConnectionString);
}
else
{
    // Register no-op embedding generators to allow app to start without Azure AI
    // Operations will throw if attempted, preventing data corruption
    builder.Services.AddNoOpEmbeddingGenerator();
}

// Add scraper services
builder.Services.Configure<ScraperConfiguration>(builder.Configuration.GetSection("Scraper"));
builder.Services.AddScoped<IWebScraperSessionFactory, WebScraperSessionFactory>();
builder.Services.AddScoped<SheinWebScraper>();

// Add background processing handlers
// Always register handlers to avoid runtime errors - they will gracefully handle missing dependencies
builder.Services.AddBackgroundProcessing(builder.Configuration.GetSection("BackgroundProcessing"))
    .AddHandler<SheinBatchHandler>(
        name: "Shein",
        maxItemsPerCycle: 5,
        purpose: WorkItemHandlerPurpose.Ingestion)
    .AddHandler<ProductPhotoImageVectorizationHandler>(
        name: "ProductPhotoImageVectorization",
        maxItemsPerCycle: 10,
        purpose: WorkItemHandlerPurpose.Internal);

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Add GraphQL server with HotChocolate first
builder.Services
    .AddGraphQLServer()
    .AddType<ProductPhotoType>()
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
