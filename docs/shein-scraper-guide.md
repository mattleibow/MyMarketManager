# Shein Scraper Quick Start

This guide shows how to use the Shein scraper to ingest order data from Shein.com.

## Prerequisites

1. Valid Shein account credentials
2. Captured cookies from authenticated browser session (use `MyMarketManager.SheinCollector` app)
3. Supplier record in database for Shein

## Cookie Capture Process

Use the `MyMarketManager.SheinCollector` MAUI app to capture cookies:

1. Launch the app on a mobile device or emulator
2. Login to Shein.com in the WebView
3. Click "Done - Collect Cookies" button
4. Cookies are saved to `shein_cookies.json`

See `src/MyMarketManager.SheinCollector/README.md` for details.

## Cookie File Format

The cookie file must be in JSON format:

```json
{
  "Id": "12345678-1234-1234-1234-123456789012",
  "SupplierId": "87654321-4321-4321-4321-210987654321",
  "Domain": "shein.com",
  "CapturedAt": "2025-10-10T00:00:00Z",
  "ExpiresAt": "2025-10-17T00:00:00Z",
  "Cookies": [
    {
      "name": "session_id",
      "value": "abc123...",
      "domain": ".shein.com",
      "path": "/",
      "secure": true,
      "httpOnly": true,
      "sameSite": "Lax"
    }
  ],
  "Metadata": {
    "user_agent": "Mozilla/5.0...",
    "platform": "android"
  }
}
```

## Using the Scraper

### Basic Usage

```csharp
using MyMarketManager.Data.Services.Scraping;

// 1. Load cookie file (from API POST, file system, etc.)
var cookieJson = await File.ReadAllTextAsync("shein_cookies.json");
var cookieFile = JsonSerializer.Deserialize<CookieFile>(cookieJson);

// 2. Create scraper instance
var scraper = new SheinScraper(dbContext, logger);

// 3. Get last successful scrape timestamp (optional)
DateTimeOffset? lastScrape = await dbContext.ScraperSessions
    .Where(s => s.SupplierId == cookieFile.SupplierId && s.Status == ScraperSessionStatus.Completed)
    .OrderByDescending(s => s.CompletedAt)
    .Select(s => s.CompletedAt)
    .FirstOrDefaultAsync();

// 4. Run the scraper
try
{
    var batchId = await scraper.ScrapeOrdersAsync(cookieFile, lastScrape, cancellationToken);
    
    Console.WriteLine($"Scraping completed. Staging batch ID: {batchId}");
}
catch (Exception ex)
{
    Console.WriteLine($"Scraping failed: {ex.Message}");
}
```

### Validating Cookies

Before scraping, validate that cookies are still valid:

```csharp
var scraper = new SheinScraper(dbContext, logger);

if (await scraper.ValidateCookiesAsync(cookieFile, cancellationToken))
{
    Console.WriteLine("Cookies are valid");
    // Proceed with scraping
}
else
{
    Console.WriteLine("Cookies expired or invalid. Re-login required.");
}
```

### Background Task Example

Run scraping as a background task:

```csharp
public class ScraperBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ScraperBackgroundService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MyMarketManagerDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<SheinScraper>>();

            // Get pending cookie files (implement your own queue/storage)
            var cookieFiles = await GetPendingCookieFiles();

            foreach (var cookieFile in cookieFiles)
            {
                var scraper = new SheinScraper(dbContext, logger);
                
                try
                {
                    await scraper.ScrapeOrdersAsync(cookieFile, null, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to scrape for supplier {SupplierId}", cookieFile.SupplierId);
                }
            }

            // Wait before next scrape cycle
            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}
```

## Scraper Configuration

The Shein scraper comes pre-configured with:

- **Domain**: `shein.com`
- **Orders List URL**: `https://shein.com/user/orders/list`
- **Order Detail Pattern**: `https://shein.com/user/orders/detail?order_id={orderId}`
- **Request Delay**: 2 seconds between requests
- **User Agent**: Chrome/Edge on macOS
- **Success Indicator**: Response contains `gbRawData` string

To view or modify the configuration:

```csharp
var scraper = new SheinScraper(dbContext, logger);
var config = scraper.Configuration;

Console.WriteLine($"Domain: {config.Domain}");
Console.WriteLine($"Request delay: {config.RequestDelayMs}ms");
Console.WriteLine($"User agent: {config.UserAgent}");
```

## Staging Batch Processing

After scraping completes, a `StagingBatch` is created with related `StagingPurchaseOrder` entities:

```csharp
// Find the staging batch
var batch = await dbContext.StagingBatches
    .Include(b => b.StagingPurchaseOrders)
    .FirstOrDefaultAsync(b => b.Id == batchId);

Console.WriteLine($"Batch status: {batch.Status}");
Console.WriteLine($"Orders found: {batch.StagingPurchaseOrders.Count}");

// Process each staged order
foreach (var order in batch.StagingPurchaseOrders)
{
    Console.WriteLine($"Order: {order.SupplierReference}");
    Console.WriteLine($"Date: {order.OrderDate}");
    Console.WriteLine($"Raw data: {order.RawData}");
    
    // TODO: Import logic to map to actual PurchaseOrder entities
}
```

## Tracking Scraper Sessions

Create a `ScraperSession` entity to track each scraping run:

```csharp
var session = new ScraperSession
{
    Id = Guid.NewGuid(),
    SupplierId = cookieFile.SupplierId,
    StartedAt = DateTimeOffset.UtcNow,
    Status = ScraperSessionStatus.Running
};
dbContext.ScraperSessions.Add(session);
await dbContext.SaveChangesAsync();

try
{
    var batchId = await scraper.ScrapeOrdersAsync(cookieFile, null, cancellationToken);
    
    session.CompletedAt = DateTimeOffset.UtcNow;
    session.Status = ScraperSessionStatus.Completed;
    session.StagingBatchId = batchId;
}
catch (Exception ex)
{
    session.Status = ScraperSessionStatus.Failed;
    session.ErrorMessage = ex.Message;
}

await dbContext.SaveChangesAsync();
```

## Error Handling

Common errors and solutions:

### Invalid Cookies
```
InvalidOperationException: Cookies are not valid or have expired
```
**Solution**: Re-capture cookies using the MAUI app.

### Rate Limiting
```
HttpRequestException: 429 Too Many Requests
```
**Solution**: Increase `RequestDelayMs` in scraper configuration or wait before retrying.

### Network Errors
```
HttpRequestException: Connection timeout
```
**Solution**: Check network connectivity. Increase `RequestTimeoutSeconds` if needed.

### HTML Parsing Errors
```
NullReferenceException: Object reference not set
```
**Solution**: Shein may have changed their HTML structure. Update selectors in `ScraperConfiguration`.

## Testing

Run the scraper tests:

```bash
dotnet test --filter "FullyQualifiedName~SheinScraperTests"
```

Test individual components:

```bash
dotnet test --filter "FullyQualifiedName~CookieFileTests"
dotnet test --filter "FullyQualifiedName~ScraperConfigurationTests"
```

## Next Steps

1. **Implement HTML Parsing**: Replace regex with HtmlAgilityPack or AngleSharp
2. **Add Order Detail Parsing**: Extract all fields from order HTML
3. **Implement Import Logic**: Map staging orders to actual `PurchaseOrder` entities
4. **Add Pagination**: Support scraping multiple pages of orders
5. **Implement API Endpoint**: Create GraphQL mutation or REST endpoint to receive cookies
6. **Add Scheduling**: Set up periodic scraping with Hangfire or Quartz.NET

## Related Documentation

- [Web Scraping Architecture](web-scraping.md) - Complete scraping infrastructure documentation
- [Data Layer](data-layer.md) - Entity Framework and database management
- [SheinCollector README](../src/MyMarketManager.SheinCollector/README.md) - Cookie capture app documentation

## Support

For issues or questions:
- Check logs for detailed error messages
- Review the test files for usage examples
- See `docs/web-scraping.md` for architecture details
