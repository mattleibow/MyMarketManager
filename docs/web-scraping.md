# Web Scraping Infrastructure

This document describes the web scraping infrastructure for ingesting data from supplier websites.

## Overview

The scraping infrastructure provides a pluggable system for extracting order data from supplier websites. It defines interfaces, base classes, and utilities that can be used to implement scrapers for different suppliers.

## Key Components

### 1. Cookie File Format (`CookieFile.cs`)

The `CookieFile` class represents captured browser cookies that are needed for authenticated scraping sessions.

**Properties:**
- `Id`: Unique identifier for the cookie file
- `SupplierId`: Links cookies to a specific supplier
- `Domain`: The domain these cookies are for (e.g., "shein.com")
- `CapturedAt`: When the cookies were captured
- `ExpiresAt`: Optional expiration timestamp
- `Cookies`: List of `CookieData` objects
- `Metadata`: Additional key-value metadata

**Cookie Data:**
Each `CookieData` object contains:
- `Name`, `Value`: Cookie name and value
- `Domain`, `Path`: Cookie scope
- `Secure`, `HttpOnly`: Security flags
- `Expires`, `SameSite`: Additional cookie attributes

**Usage:**
Cookies are captured by the MAUI application (`MyMarketManager.SheinCollector`) and POSTed to the server. They are then deserialized and passed to scrapers.

### 2. Scraper Interface (`IWebScraper.cs`)

The `IWebScraper` interface defines the contract that all web scrapers must implement.

**Key Methods:**

- `ScrapeOrdersAsync()`: Main entry point for scraping. Takes cookies and last scrape timestamp, returns a staging batch ID.
- `ValidateCookiesAsync()`: Checks if cookies are still valid by loading a protected page.
- `ScrapePageAsync()`: Fetches a specific page type using cookies.
- `ExtractOrderLinks()`: Parses the orders list page to find order detail URLs.
- `ParseOrderDetailsAsync()`: Parses an order detail page into structured data.

**Page Types:**
- `ProductPage`: Individual product detail pages
- `OrdersListPage`: Page listing all orders
- `OrderDetailsPage`: Detailed view of a single order
- `AccountPage`: User account page (for validation)

### 3. Scraper Configuration (`ScraperConfiguration.cs`)

Each scraper has a configuration object that defines:

**URLs:**
- `OrdersListUrl`: URL for the orders list
- `OrderDetailUrlPattern`: Pattern for order detail URLs (with placeholders like `{orderId}`)
- `ProductPageUrlPattern`: Pattern for product pages
- `AccountPageUrl`: URL for validation

**Selectors:**
- `OrderLinkSelector`: CSS/XPath selector for order links
- `OrderIdSelector`, `OrderDateSelector`, `OrderItemsSelector`: Selectors for parsing order data
- `LoggedInIndicatorSelector`: Selector to verify authentication

**HTTP Settings:**
- `UserAgent`: User agent string to use
- `AdditionalHeaders`: Extra headers to send
- `RequestDelayMs`: Delay between requests (rate limiting)
- `MaxConcurrentRequests`: Concurrent request limit
- `RequestTimeoutSeconds`: Request timeout
- `RequiresHeadlessBrowser`: Whether JavaScript rendering is needed

### 4. Shein Scraper (`SheinScraper.cs`)

The `SheinScraper` is the first implementation of `IWebScraper` for Shein.com.

**Pre-configured for Shein:**
- Domain: `shein.com`
- Orders list URL: `https://shein.com/user/orders/list`
- Order detail pattern: `https://shein.com/user/orders/detail?order_id={orderId}`
- User agent: Chrome/Edge on macOS
- Request delay: 2000ms (2 seconds)
- Success indicator: Response contains `gbRawData`

**Scraping Process:**

1. **Validate Cookies**: Check cookies are valid by loading account page
2. **Create Staging Batch**: Create a new `StagingBatch` entity
3. **Fetch Orders List**: Load the orders list page with cookies
4. **Extract Order Links**: Parse HTML to find order detail URLs
5. **Scrape Each Order**: For each order URL:
   - Wait for rate limit delay
   - Fetch the order detail page
   - Parse the page into structured data
   - Create a `StagingPurchaseOrder` entity
6. **Save to Database**: Persist all staging entities

**Notes:**
- Currently uses simple regex for HTML parsing (TODO: migrate to proper HTML parser like HtmlAgilityPack)
- Only basic order data extraction implemented (placeholder for full parsing)
- Designed to be run as a background task/job

### 5. Scraper Session Entity (`ScraperSession.cs`)

The `ScraperSession` entity tracks scraping sessions in the database.

**Properties:**
- `SupplierId`: Which supplier was scraped
- `StartedAt`, `CompletedAt`: Timing information
- `Status`: Queued, Running, Completed, Failed, or Cancelled
- `StagingBatchId`: Link to the created staging batch
- `LastSuccessfulScrape`: Previous scrape timestamp (for incremental scraping)
- `PagesScraped`, `OrdersScraped`: Statistics
- `ErrorMessage`: Error details if failed

## Usage Example

```csharp
// 1. Receive cookie file from client (e.g., via API POST)
var cookieFile = JsonSerializer.Deserialize<CookieFile>(requestBody);

// 2. Get last successful scrape timestamp
var lastScrape = await context.ScraperSessions
    .Where(s => s.SupplierId == cookieFile.SupplierId && s.Status == ScraperSessionStatus.Completed)
    .OrderByDescending(s => s.CompletedAt)
    .Select(s => s.CompletedAt)
    .FirstOrDefaultAsync();

// 3. Create scraper
var scraper = new SheinScraper(context, logger);

// 4. Scrape orders
var batchId = await scraper.ScrapeOrdersAsync(cookieFile, lastScrape, cancellationToken);

// 5. Process staging batch
// (Import logic runs separately to map staging data to actual entities)
```

## Implementing a New Scraper

To add support for a new supplier:

1. **Create a new class** implementing `IWebScraper`
2. **Define the configuration** with URLs and selectors specific to that supplier
3. **Implement extraction logic**:
   - `ExtractOrderLinks()`: Parse the order list HTML
   - `ParseOrderDetailsAsync()`: Parse order detail HTML
4. **Test with real cookies** from that supplier
5. **Add unit tests** for the new scraper

Example skeleton:

```csharp
public class MySupplierScraper : IWebScraper
{
    public ScraperConfiguration Configuration { get; } = new ScraperConfiguration
    {
        SupplierName = "MySupplier",
        Domain = "mysupplier.com",
        OrdersListUrl = "https://mysupplier.com/orders",
        // ... other configuration
    };

    public async Task<Guid> ScrapeOrdersAsync(CookieFile cookieFile, DateTimeOffset? lastSuccessfulScrape, CancellationToken cancellationToken)
    {
        // Implementation
    }

    // ... other interface methods
}
```

## Future Enhancements

### Short Term
- [ ] Replace regex-based HTML parsing with HtmlAgilityPack or AngleSharp
- [ ] Implement full order detail parsing (extract all order fields)
- [ ] Add retry logic with exponential backoff
- [ ] Implement pagination support for large order lists
- [ ] Add progress reporting/callbacks

### Medium Term
- [ ] Support for headless browser (Playwright/Selenium) for JavaScript-heavy sites
- [ ] Cookie refresh/renewal flow
- [ ] Incremental scraping (only fetch new orders since last scrape)
- [ ] Scraper health monitoring and alerting

### Long Term
- [ ] Multi-supplier concurrent scraping
- [ ] Rate limit management across scrapers
- [ ] Scraper marketplace/plugin system
- [ ] ML-based HTML parsing (auto-detect selectors)

## Testing

Unit tests are located in `tests/MyMarketManager.Data.Tests/Services/`:

- `CookieFileTests.cs`: Tests cookie file serialization and deserialization
- `ScraperConfigurationTests.cs`: Tests scraper configuration
- `SheinScraperTests.cs`: Tests Shein scraper implementation

Run tests with:
```bash
dotnet test --filter "FullyQualifiedName~CookieFileTests"
dotnet test --filter "FullyQualifiedName~ScraperConfigurationTests"
dotnet test --filter "FullyQualifiedName~SheinScraperTests"
```

## Security Considerations

- **Cookie Storage**: Cookies contain sensitive authentication data. Store securely and encrypt if persisted.
- **Rate Limiting**: Respect supplier rate limits to avoid IP bans.
- **User Agent**: Use realistic user agents to avoid detection.
- **Robots.txt**: Check and respect robots.txt policies where applicable.
- **Terms of Service**: Ensure scraping complies with supplier terms of service.
- **Personal Data**: Handle scraped data in compliance with privacy regulations (GDPR, etc.).

## Related Files

- Cookie capture: `src/MyMarketManager.SheinCollector/CookieService.cs`
- Data entities: `src/MyMarketManager.Data/Entities/`
- Staging batch: `src/MyMarketManager.Data/Entities/StagingBatch.cs`
- Scraper session: `src/MyMarketManager.Data/Entities/ScraperSession.cs`
