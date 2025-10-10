# Scraper Configuration Example

## appsettings.json Configuration

The scraper configuration is stored in `appsettings.json` under the `WebScrapers` section. Each scraper has its own named configuration:

```json
{
  "WebScrapers": {
    "Shein": {
      "SupplierName": "Shein",
      "Domain": "shein.com",
      "OrdersListUrlTemplate": "https://shein.com/user/orders/list",
      "OrderDetailUrlTemplate": "https://shein.com/user/orders/detail?order_id={orderId}",
      "ProductPageUrlTemplate": "https://shein.com/product/{productId}",
      "AccountPageUrlTemplate": "https://shein.com/user/account",
      "UserAgent": "Mozilla/5.0...",
      "AdditionalHeaders": {
        "accept": "text/html",
        "accept-language": "en-US"
      },
      "RequestDelay": "00:00:02",
      "MaxConcurrentRequests": 1,
      "RequestTimeout": "00:00:30",
      "RequiresHeadlessBrowser": false
    }
  }
}
```

Note: TimeSpan values are specified in the format "hh:mm:ss" (e.g., "00:00:02" for 2 seconds).

## Registering the Scraper in Program.cs

```csharp
using MyMarketManager.Data.Services.Scraping;

var builder = WebApplication.CreateBuilder(args);

// Register scraper configuration from appsettings
builder.Services.Configure<ScraperConfiguration>(
    builder.Configuration.GetSection("WebScrapers:Shein"));

// Register the scraper
builder.Services.AddScoped<SheinScraper>();

var app = builder.Build();
// ... rest of the configuration
```

## Using the Scraper

```csharp
public class ScraperService
{
    private readonly SheinScraper _scraper;

    public ScraperService(SheinScraper scraper)
    {
        _scraper = scraper;
    }

    public async Task RunScraperAsync(CookieFile cookieFile)
    {
        // Initialize the scraper with cookies
        await _scraper.InitializeAsync(cookieFile);

        // Run the scraper
        await _scraper.ScrapeAsync();
    }
}
```

## Adding Multiple Scrapers

To support multiple suppliers, register each with its own configuration:

```csharp
// Register Shein scraper
builder.Services.Configure<ScraperConfiguration>(
    "Shein",
    builder.Configuration.GetSection("WebScrapers:Shein"));
builder.Services.AddScoped<SheinScraper>();

// Register another scraper (example)
builder.Services.Configure<ScraperConfiguration>(
    "AnotherSupplier",
    builder.Configuration.GetSection("WebScrapers:AnotherSupplier"));
builder.Services.AddScoped<AnotherSupplierScraper>();
```

## Accessing Configuration in Scraper

The configuration is automatically injected into the scraper constructor via `IOptions<ScraperConfiguration>`:

```csharp
public class SheinScraper : WebScraperBase
{
    public SheinScraper(
        MyMarketManagerDbContext context,
        ILogger<SheinScraper> logger,
        IOptions<ScraperConfiguration> configuration) // Configuration injected here
        : base(context, logger, configuration)
    {
    }
}
```

The base class extracts the configuration value:

```csharp
protected readonly ScraperConfiguration Configuration;

protected WebScraperBase(
    MyMarketManagerDbContext context,
    ILogger logger,
    IOptions<ScraperConfiguration> configuration)
{
    Context = context;
    Logger = logger;
    Configuration = configuration.Value; // Access the configuration
}
```

## Configuration Properties

| Property | Type | Description |
|----------|------|-------------|
| SupplierName | string | Human-readable supplier name |
| Domain | string | Base domain (e.g., "shein.com") |
| OrdersListUrlTemplate | string | URL template for orders list page |
| OrderDetailUrlTemplate | string | URL template with placeholders (e.g., `{orderId}`) |
| ProductPageUrlTemplate | string | URL template for product pages |
| AccountPageUrlTemplate | string | URL for account/validation page |
| UserAgent | string | User agent string for HTTP requests |
| AdditionalHeaders | Dictionary<string, string> | Extra HTTP headers |
| RequestDelay | TimeSpan | Delay between requests (e.g., "00:00:02" = 2 seconds) |
| MaxConcurrentRequests | int | Max number of concurrent requests |
| RequestTimeout | TimeSpan | HTTP request timeout |
| RequiresHeadlessBrowser | bool | Whether to use a headless browser |
| Notes | string | Optional notes about the scraper |
