# Web Scraping Infrastructure

The web scraping system extracts order data from supplier websites using authenticated sessions. It provides a pluggable architecture for implementing supplier-specific scrapers.

## Architecture

The system has two main projects:

- **MyMarketManager.Scrapers.Core** - Cookie file format for authentication
- **MyMarketManager.Scrapers** - Scraper framework and implementations

### Core Components

**Cookie Management** (`CookieFile`, `CookieData`)
- Represents captured browser cookies needed for authenticated scraping
- Properties: `Domain`, `CapturedAt`, `ExpiresAt`, `Cookies` dictionary, `Metadata`
- Serialized to JSON and stored in `StagingBatch.FileContents`

**Session Management** (`IWebScraperSession`, `WebScraperSession`)
- Wraps HttpClient with configured cookies and headers
- Single method: `FetchPageAsync(url)` to retrieve HTML content
- Disposable to release HTTP resources

**Session Factory** (`IWebScraperSessionFactory`, `WebScraperSessionFactory`)
- Creates scraping sessions from cookie files
- Configures HttpClient with cookies, user agent, and custom headers
- Applies `ScraperConfiguration` settings

**Scraper Configuration** (`ScraperConfiguration`)
- HTTP settings: `UserAgent`, `AdditionalHeaders`, `RequestTimeout`
- Rate limiting: `RequestDelay` (default 1 second), `MaxConcurrentRequests`
- Loaded from application configuration

### Base Scraper Class

`WebScraper` is an abstract base class providing the scraping orchestration:

**Required Override Methods:**
- `GetOrdersListUrl()` - Returns the URL for the orders list page
- `GetOrderDetailUrl(orderSummary)` - Builds order detail URL from summary data
- `ParseOrdersListAsync(html)` - Extracts order summaries from list page HTML
- `ParseOrderDetailsAsync(html, orderSummary)` - Parses full order details
- `UpdateStagingPurchaseOrderAsync(stagingOrder, order)` - Maps scraped data to staging entity

**Scraping Workflow (provided by base class):**
1. Creates `StagingBatch` with `BatchType.WebScrape`
2. Creates scraping session with cookies
3. Fetches orders list page
4. Parses order summaries (async enumerable)
5. For each order:
   - Creates `StagingPurchaseOrder` 
   - Fetches detail page with rate limiting
   - Parses details and updates staging order
6. Marks batch as completed or failed

**Data Transfer Objects:**
- `WebScraperOrderSummary` - Dictionary of fields from orders list (e.g., `orderNumber`)
- `WebScraperOrder` - Dictionary of order fields plus `OrderItems` list and `RawData`
- `WebScraperOrderItem` - Dictionary of item fields plus `RawData`

Using dictionaries allows each scraper to define supplier-specific fields without changing the framework.

## Shein Scraper Implementation

`SheinWebScraper` extends `WebScraper` for Shein.com:

- **Orders List**: `https://shein.com/user/orders/list`
- **Order Detail**: `https://shein.com/user/orders/detail/{orderNumber}`
- **Parsing**: Extracts `gbRawData` JavaScript object from HTML and parses as JSON
- **Order Fields**: `billno`, `addTime`, `pay_time`, `totalPrice`, `currency_code`
- **Item Fields**: `goods_id`, `goods_name`, `goods_sn`, `goods_price`, `goods_qty`, `sku_attributes`

The scraper extracts structured JSON embedded in the HTML rather than parsing DOM elements.

## Usage

Scrapers integrate with the staging batch workflow:

1. Cookie file captured by client app (e.g., MAUI SheinCollector)
2. Call `scraper.StartScrapingAsync(supplierId, cookieFile)`
3. Scraper creates and populates `StagingBatch` with `StagingPurchaseOrder` entities
4. Staging data reviewed and imported separately

## Implementing New Scrapers

To add a supplier scraper:

1. Create class extending `WebScraper`
2. Implement abstract methods for URLs and parsing
3. Add unit tests with HTML fixtures
4. Register scraper in DI container

The base class handles all orchestration, error handling, and database persistence.

## Testing

Tests use mocked `IWebScraperSession` with HTML fixtures:

- `tests/MyMarketManager.Scrapers.Tests/` - Scraper implementation tests
- `tests/MyMarketManager.Scrapers.Core.Tests/` - Cookie file tests
- `tests/MyMarketManager.Scrapers.Tests/Fixtures/Html/` - HTML test data

Run tests:
```bash
dotnet test --filter "FullyQualifiedName~Scraper"
```

## Security

- **Authentication**: Cookies contain session tokens - handle securely
- **Rate Limiting**: Default 1 second delay between requests
- **User Agent**: Realistic browser UA to avoid detection
- **Terms of Service**: Ensure scraping complies with supplier ToS
- **Data Privacy**: Handle scraped data per applicable regulations
