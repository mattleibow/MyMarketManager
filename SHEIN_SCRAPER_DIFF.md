# Shein Scraper Feature - Code Changes Summary

This PR summarizes the source code changes from the `origin/copilot/add-shein-scraper-functionality` branch compared to `main` (excludes docs, migrations, and tests).

## What Changed

### New Projects (2)
- **MyMarketManager.Scrapers.Core**: Cookie data models (`CookieData`, `CookieFile`)
- **MyMarketManager.Scrapers**: Scraping infrastructure and Shein implementation

### Key Files Added (17 total)

**Scrapers.Core:**
- `CookieData.cs` - HTTP cookie representation
- `CookieFile.cs` - Serializable cookie container with metadata

**Scrapers:**
- `WebScraper.cs` - Abstract base class with template method pattern
- `SheinWebScraper.cs` - Shein.com implementation, parses `gbRawData` JSON
- `IWebScraperSession.cs` / `WebScraperSession.cs` - HTTP session management
- `IWebScraperSessionFactory.cs` / `WebScraperSessionFactory.cs` - Session factory with cookie injection
- `ScraperConfiguration.cs` - HTTP settings (user agent, headers, timeouts)
- `WebScraperOrder*.cs` - Data models for order/item parsing

### Data Model Changes

**ProcessingStatus enum** - Expanded 3→5 states:
- Old: `Pending`, `Partial`, `Complete`
- New: `Queued`, `Started`, `Completed`, `Failed`, `Cancelled`

**StagingBatchType enum** (new):
- `WebScrape` - Data from web scraping
- `BlobUpload` - Data from file upload

**StagingBatch entity** - Enhanced for web scraping:
- Added: `BatchType`, `CompletedAt`, `ErrorMessage`, `FileContents`
- Changed: `SupplierId` now nullable, `UploadDate` → `StartedAt`

**StagingPurchaseOrder entity** - Added status tracking:
- Added: `Status`, `ErrorMessage`

### MAUI App Updates

**SheinCollector:**
- `CookieService.cs` - New `CreateCookieFileJson()` method
- `MainPage.xaml` - Added "Copy JSON" button
- `MainPage.xaml.cs` - Cookie caching and clipboard copy functionality

### Configuration

**appsettings.json** - New `ScraperConfiguration` section with user agent, headers, request delay (2s), and timeout (30s)

## How It Works

1. **Cookie Collection**: User logs into Shein via MAUI WebView → clicks "Copy JSON" → gets `CookieFile` JSON
2. **Scraping**: Backend receives JSON → creates `StagingBatch` (type: WebScrape) → `SheinWebScraper` fetches orders list → parses `gbRawData` → fetches each order detail → creates `StagingPurchaseOrder` + items
3. **Data Flow**: Raw JSON stored in `RawData` → parsed into staging entities → processed by existing import workflow

## Extensibility

To add new supplier scrapers, inherit from `WebScraper` and implement 5 abstract methods:
- `GetOrdersListUrl()`, `GetOrderDetailUrl()`, `ParseOrdersListAsync()`, `ParseOrderDetailsAsync()`, `UpdateStagingPurchaseOrderAsync()`

## Stats

- **New files**: 17 source files (~1,000+ lines)
- **Modified files**: 6 (StagingBatch, StagingPurchaseOrder, CookieService, MainPage, appsettings, .gitignore)
- **New enums**: 1 (StagingBatchType)
- **Modified enums**: 1 (ProcessingStatus)
- **Design patterns**: Template Method, Factory, Async Streams
