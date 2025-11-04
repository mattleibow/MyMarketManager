# Shein Scraper Usage

This guide shows how to use the Shein scraper to extract order data.

## Prerequisites

- Valid Shein account with order history
- Captured cookies from authenticated browser session
- Supplier record in database for Shein

## Cookie Capture

Use the `MyMarketManager.SheinCollector` MAUI app:

1. Launch app and login to Shein.com in WebView
2. Click "Done - Collect Cookies"
3. Cookies saved as JSON in `CookieFile` format

See `src/MyMarketManager.SheinCollector/README.md` for details.

## CookieFile Format

```json
{
  "domain": "shein.com",
  "capturedAt": "2025-10-10T00:00:00Z",
  "expiresAt": "2025-10-17T00:00:00Z",
  "cookies": {
    "session_id": {
      "name": "session_id",
      "value": "abc123...",
      "domain": ".shein.com",
      "path": "/",
      "secure": true,
      "httpOnly": true
    }
  }
}
```

## Basic Usage

```csharp
using MyMarketManager.Scrapers;
using MyMarketManager.Scrapers.Core;

// 1. Load cookie file
var cookieJson = await File.ReadAllTextAsync("cookies.json");
var cookies = JsonSerializer.Deserialize<CookieFile>(cookieJson);

// 2. Create scraper with dependencies
var scraper = new SheinWebScraper(
    dbContext, 
    logger, 
    Options.Create(new ScraperConfiguration()),
    sessionFactory);

// 3. Start scraping
await scraper.StartScrapingAsync(supplierId, cookies, cancellationToken);
```

## How It Works

The scraper:
1. Creates a `StagingBatch`
2. Fetches `https://shein.com/user/orders/list` with cookies
3. Extracts `gbRawData` JSON from HTML and parses order numbers
4. For each order:
   - Fetches `https://shein.com/user/orders/detail/{orderNumber}`
   - Parses `gbRawData` JSON for order details and items
   - Creates `StagingPurchaseOrder` with items
5. Marks batch as completed

## Scraped Data

**Order Fields:**
- `billno` - Order number (supplier reference)
- `addTime` - Order date (Unix timestamp)
- `pay_time` - Payment timestamp
- `totalPrice.amount` - Total order amount
- `currency_code` - Currency

**Item Fields:**
- `goods_sn` - SKU/item reference
- `goods_name` - Product name
- `goods_qty` - Quantity ordered
- `goods_unit_price` - Unit price
- `sku_attributes` - Color, size, etc.
- `goods_url_name` - Product page slug

All scraped data stored as JSON in `StagingPurchaseOrder.RawData` and `StagingPurchaseOrderItem.RawData`.

## Processing Staging Data

After scraping, review and import staging data:

```csharp
var batch = await context.StagingBatches
    .Include(b => b.StagingPurchaseOrders)
        .ThenInclude(o => o.Items)
    .FirstAsync(b => b.Id == batchId);

foreach (var order in batch.StagingPurchaseOrders)
{
    // Review and import to PurchaseOrder entities
    Console.WriteLine($"Order: {order.SupplierReference}");
    Console.WriteLine($"Items: {order.Items.Count}");
}
```

## Configuration

Default configuration from `ScraperConfiguration`:
- **Request Delay**: 1 second
- **Request Timeout**: 30 seconds
- **User Agent**: Chrome/Edge on macOS

Override via application configuration or DI.

## Testing

Run scraper tests:

```bash
dotnet test --filter "FullyQualifiedName~SheinWebScraper"
```

Tests use HTML fixtures from real Shein pages (sanitized) in `tests/MyMarketManager.Scrapers.Tests/Fixtures/Html/`.

## Common Issues

**Invalid Cookies**
- Cookies expire after ~7 days
- Re-capture using SheinCollector app

**Rate Limiting**
- Default 1 second delay should prevent issues
- Increase via `ScraperConfiguration.RequestDelay` if needed

**Parsing Errors**
- Shein may change HTML/JSON structure
- Check `gbRawData` extraction in `SheinWebScraper.ExtractGbRawData()`
- Update JSON property paths if needed
