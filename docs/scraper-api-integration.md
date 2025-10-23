# Scraper API Integration

This document outlines how to integrate the web scraper into your application's API layer.

## Overview

The scraper infrastructure is designed to be integrated via:
- GraphQL mutations for scraping operations
- Background services for async processing
- Cookie storage for authentication persistence

## Integration Pattern

**Client → API → Background Service → Database**

1. Client (MAUI app) captures cookies and submits to API
2. API creates staging batch record and queues scraping job
3. Background service processes queue and runs scrapers
4. Staging data available for review and import

## Key Components

**Cookie Submission Endpoint**
- Accepts `CookieFile` JSON
- Validates supplier exists
- Creates `StagingBatch` with cookies in `FileContents`
- Returns batch ID for tracking

**Background Processing**
- Monitors `StagingBatch` table for unprocessed batches
- Creates appropriate scraper instance based on supplier
- Calls `scraper.ScrapeBatchAsync(batch)`
- Updates batch status (Completed/Failed)

**Cookie Storage Options**
- **Simple**: Store in `StagingBatch.FileContents` as JSON
- **Secure**: Encrypt cookies before storing
- **Scalable**: Use blob storage or Redis for large deployments

## GraphQL Integration

Add mutation to accept cookies:

```csharp
public async Task<Guid> SubmitCookiesForScraping(
    Guid supplierId,
    CookieFile cookies,
    [Service] MyMarketManagerDbContext context)
{
    // Create staging batch with cookies
    var batch = new StagingBatch
    {
        Id = Guid.NewGuid(),
        SupplierId = supplierId,
        BatchType = StagingBatchType.WebScrape,
        FileContents = JsonSerializer.Serialize(cookies),
        Status = ProcessingStatus.Started
    };
    context.StagingBatches.Add(batch);
    await context.SaveChangesAsync();
    
    return batch.Id;
}
```

Background service picks up and processes batches.

## REST Alternative

```csharp
[HttpPost("api/scraper/cookies")]
public async Task<IActionResult> SubmitCookies([FromBody] CookieFile cookies)
{
    // Same logic as GraphQL mutation
}
```

## Background Service Pattern

```csharp
public class ScraperBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessPendingBatches(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task ProcessPendingBatches(CancellationToken cancellationToken)
    {
        // Find batches with Status = Started
        // Create scraper based on supplier
        // Call scraper.ScrapeBatchAsync(batch)
        // Update batch status
    }
}
```

Register in `Program.cs`:
```csharp
builder.Services.AddHostedService<ScraperBackgroundService>();
```

## Security Considerations

**Encrypt Cookies**
```csharp
// Before storing
var encrypted = EncryptCookies(cookies);
batch.FileContents = encrypted;

// Before using
var cookies = DecryptCookies(batch.FileContents);
```

Use ASP.NET Core Data Protection API for encryption.

**Validate Supplier Access**
- Ensure user has permission to scrape for supplier
- Rate limit scraping requests per supplier
- Log all scraping operations for audit

## Testing

Test the integration:

```bash
# Submit cookies via GraphQL
mutation {
  submitCookies(supplierId: "...", cookies: {...}) {
    batchId
  }
}

# Check batch status
query {
  stagingBatch(id: "...") {
    status
    stagingPurchaseOrders { count }
  }
}
```

## Error Handling

Common scenarios:
- **Invalid supplier**: Return validation error
- **Expired cookies**: Background service marks batch as failed
- **Network issues**: Retry logic in scraper with exponential backoff
- **Parse failures**: Individual orders fail, batch may partially succeed

Monitor `StagingBatch.Status` and `StagingBatch.ErrorMessage` for failures.

## Related Documentation

- [Web Scraping](web-scraping.md) - Architecture and components
- [Shein Scraper Guide](shein-scraper-guide.md) - Using the Shein scraper
