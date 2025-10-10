# Sample API Integration for Scraper

This document shows example code for integrating the web scraper into the API layer.

## GraphQL Mutation Example

Here's how you could add a GraphQL mutation to trigger scraping:

```csharp
// File: src/MyMarketManager.WebApp/GraphQL/ScraperMutations.cs

using HotChocolate;
using HotChocolate.Types;
using MyMarketManager.Data;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Services.Scraping;
using Microsoft.EntityFrameworkCore;

namespace MyMarketManager.WebApp.GraphQL;

[ExtendObjectType(typeof(Mutation))]
public class ScraperMutations
{
    /// <summary>
    /// Submit cookies for web scraping. The scraping will run asynchronously.
    /// </summary>
    public async Task<SubmitCookiesPayload> SubmitCookiesForScraping(
        SubmitCookiesInput input,
        [Service] MyMarketManagerDbContext context,
        [Service] ILogger<ScraperMutations> logger,
        CancellationToken cancellationToken)
    {
        // Validate supplier exists
        var supplier = await context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == input.SupplierId, cancellationToken);
        
        if (supplier == null)
        {
            return new SubmitCookiesPayload
            {
                Success = false,
                ErrorMessage = $"Supplier with ID {input.SupplierId} not found"
            };
        }

        // Deserialize cookie file
        var cookieFile = new CookieFile
        {
            SupplierId = input.SupplierId,
            Domain = input.Domain,
            CapturedAt = DateTimeOffset.UtcNow,
            ExpiresAt = input.ExpiresAt,
            Cookies = input.Cookies,
            Metadata = input.Metadata ?? new Dictionary<string, string>()
        };

        // Create scraper session
        var session = new ScraperSession
        {
            Id = Guid.NewGuid(),
            SupplierId = input.SupplierId,
            StartedAt = DateTimeOffset.UtcNow,
            Status = ScraperSessionStatus.Queued,
            Notes = $"Submitted via API for {supplier.Name}"
        };

        context.ScraperSessions.Add(session);
        await context.SaveChangesAsync(cancellationToken);

        // Queue scraping job (use a background service, Hangfire, etc.)
        // For now, just return the session ID
        logger.LogInformation("Cookie submission created scraper session {SessionId}", session.Id);

        return new SubmitCookiesPayload
        {
            Success = true,
            ScraperSessionId = session.Id,
            Message = $"Scraping session queued for {supplier.Name}"
        };
    }

    /// <summary>
    /// Get the status of a scraper session.
    /// </summary>
    public async Task<ScraperSessionStatus?> GetScraperSessionStatus(
        Guid sessionId,
        [Service] MyMarketManagerDbContext context,
        CancellationToken cancellationToken)
    {
        var session = await context.ScraperSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        return session?.Status;
    }

    /// <summary>
    /// Get details of a scraper session.
    /// </summary>
    public async Task<ScraperSession?> GetScraperSession(
        Guid sessionId,
        [Service] MyMarketManagerDbContext context,
        CancellationToken cancellationToken)
    {
        return await context.ScraperSessions
            .Include(s => s.StagingBatch)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
    }
}

// Input/Output types
public record SubmitCookiesInput
{
    public Guid SupplierId { get; init; }
    public string Domain { get; init; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; init; }
    public List<CookieData> Cookies { get; init; } = new();
    public Dictionary<string, string>? Metadata { get; init; }
}

public record SubmitCookiesPayload
{
    public bool Success { get; init; }
    public Guid? ScraperSessionId { get; init; }
    public string? Message { get; init; }
    public string? ErrorMessage { get; init; }
}
```

## Background Service Example

Process queued scraping jobs in a background service:

```csharp
// File: src/MyMarketManager.WebApp/Services/ScraperBackgroundService.cs

using Microsoft.Extensions.Hosting;
using MyMarketManager.Data;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Services.Scraping;
using Microsoft.EntityFrameworkCore;

namespace MyMarketManager.WebApp.Services;

public class ScraperBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ScraperBackgroundService> _logger;

    public ScraperBackgroundService(
        IServiceProvider services,
        ILogger<ScraperBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scraper background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueuedSessions(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scraper queue");
            }

            // Check for new sessions every 30 seconds
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task ProcessQueuedSessions(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyMarketManagerDbContext>();

        // Get queued sessions
        var queuedSessions = await dbContext.ScraperSessions
            .Include(s => s.Supplier)
            .Where(s => s.Status == ScraperSessionStatus.Queued)
            .OrderBy(s => s.StartedAt)
            .Take(5) // Process up to 5 at a time
            .ToListAsync(cancellationToken);

        foreach (var session in queuedSessions)
        {
            try
            {
                await ProcessSession(session, dbContext, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process session {SessionId}", session.Id);
                
                session.Status = ScraperSessionStatus.Failed;
                session.ErrorMessage = ex.Message;
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private async Task ProcessSession(
        ScraperSession session,
        MyMarketManagerDbContext dbContext,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing scraper session {SessionId}", session.Id);

        // Update status to running
        session.Status = ScraperSessionStatus.Running;
        await dbContext.SaveChangesAsync(cancellationToken);

        // TODO: Retrieve cookie file from storage
        // For now, this is a placeholder
        var cookieFile = await GetCookieFileForSession(session);

        if (cookieFile == null)
        {
            throw new InvalidOperationException("Cookie file not found for session");
        }

        // Create appropriate scraper based on supplier
        IWebScraper scraper = session.Supplier.Name.ToLowerInvariant() switch
        {
            "shein" => new SheinScraper(
                dbContext,
                _services.GetRequiredService<ILogger<SheinScraper>>()),
            _ => throw new NotSupportedException($"No scraper available for supplier {session.Supplier.Name}")
        };

        // Get last successful scrape
        var lastScrape = await dbContext.ScraperSessions
            .Where(s => s.SupplierId == session.SupplierId 
                     && s.Status == ScraperSessionStatus.Completed
                     && s.Id != session.Id)
            .OrderByDescending(s => s.CompletedAt)
            .Select(s => s.CompletedAt)
            .FirstOrDefaultAsync(cancellationToken);

        // Run scraper
        var batchId = await scraper.ScrapeOrdersAsync(cookieFile, lastScrape, cancellationToken);

        // Update session
        session.Status = ScraperSessionStatus.Completed;
        session.CompletedAt = DateTimeOffset.UtcNow;
        session.StagingBatchId = batchId;
        
        // Update statistics from staging batch
        var batch = await dbContext.StagingBatches
            .Include(b => b.StagingPurchaseOrders)
            .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken);
        
        if (batch != null)
        {
            session.OrdersScraped = batch.StagingPurchaseOrders.Count;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Completed scraper session {SessionId}. Orders scraped: {Count}",
            session.Id,
            session.OrdersScraped);
    }

    private async Task<CookieFile?> GetCookieFileForSession(ScraperSession session)
    {
        // TODO: Implement cookie file retrieval from:
        // - Database (store CookieFile JSON in session)
        // - File system
        // - Blob storage
        // - Redis cache
        // etc.
        
        await Task.CompletedTask;
        return null;
    }
}
```

## Service Registration

Register the scraper services in `Program.cs`:

```csharp
// File: src/MyMarketManager.WebApp/Program.cs

// Add scraper background service
builder.Services.AddHostedService<ScraperBackgroundService>();

// Optionally register scrapers as services
builder.Services.AddScoped<SheinScraper>();

// Add to GraphQL schema
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddTypeExtension<ScraperMutations>()  // Add this line
    // ... other configuration
```

## REST API Alternative

If you prefer REST over GraphQL:

```csharp
// File: src/MyMarketManager.WebApp/Controllers/ScraperController.cs

using Microsoft.AspNetCore.Mvc;
using MyMarketManager.Data;
using MyMarketManager.Data.Services.Scraping;
using MyMarketManager.Data.Entities;

namespace MyMarketManager.WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScraperController : ControllerBase
{
    private readonly MyMarketManagerDbContext _context;
    private readonly ILogger<ScraperController> _logger;

    public ScraperController(
        MyMarketManagerDbContext context,
        ILogger<ScraperController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost("cookies")]
    public async Task<IActionResult> SubmitCookies([FromBody] CookieFile cookieFile)
    {
        // Validate supplier exists
        var supplier = await _context.Suppliers.FindAsync(cookieFile.SupplierId);
        if (supplier == null)
        {
            return BadRequest($"Supplier {cookieFile.SupplierId} not found");
        }

        // Create scraper session
        var session = new ScraperSession
        {
            Id = Guid.NewGuid(),
            SupplierId = cookieFile.SupplierId,
            StartedAt = DateTimeOffset.UtcNow,
            Status = ScraperSessionStatus.Queued
        };

        _context.ScraperSessions.Add(session);
        await _context.SaveChangesAsync();

        // TODO: Store cookie file for background processing
        // TODO: Queue background job

        return Ok(new
        {
            sessionId = session.Id,
            message = "Scraping session queued"
        });
    }

    [HttpGet("sessions/{sessionId}/status")]
    public async Task<IActionResult> GetSessionStatus(Guid sessionId)
    {
        var session = await _context.ScraperSessions.FindAsync(sessionId);
        
        if (session == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            sessionId = session.Id,
            status = session.Status.ToString(),
            startedAt = session.StartedAt,
            completedAt = session.CompletedAt,
            ordersScraped = session.OrdersScraped,
            errorMessage = session.ErrorMessage
        });
    }
}
```

## Storage Options for Cookie Files

### Option 1: Store in ScraperSession (Simple)

```csharp
// Add to ScraperSession entity
public class ScraperSession : EntityBase
{
    // ... existing properties
    
    /// <summary>
    /// Serialized cookie file JSON.
    /// </summary>
    public string? CookieFileJson { get; set; }
}

// Store cookies
session.CookieFileJson = JsonSerializer.Serialize(cookieFile);

// Retrieve cookies
var cookieFile = JsonSerializer.Deserialize<CookieFile>(session.CookieFileJson);
```

### Option 2: Blob Storage (Scalable)

```csharp
public interface ICookieStorage
{
    Task<string> StoreAsync(CookieFile cookieFile);
    Task<CookieFile?> RetrieveAsync(string key);
    Task DeleteAsync(string key);
}

public class BlobCookieStorage : ICookieStorage
{
    // Use Azure Blob Storage, AWS S3, or local file system
}
```

### Option 3: Redis Cache (Fast)

```csharp
public class RedisCookieStorage : ICookieStorage
{
    private readonly IDistributedCache _cache;
    
    public async Task<string> StoreAsync(CookieFile cookieFile)
    {
        var key = $"cookies:{cookieFile.Id}";
        var json = JsonSerializer.Serialize(cookieFile);
        
        await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
        });
        
        return key;
    }
}
```

## Security Considerations

### Encrypt Sensitive Cookies

```csharp
using System.Security.Cryptography;
using System.Text;

public class EncryptedCookieStorage
{
    private readonly byte[] _key;
    
    public string Encrypt(CookieFile cookieFile)
    {
        var json = JsonSerializer.Serialize(cookieFile);
        
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        
        using var encryptor = aes.CreateEncryptor();
        var encrypted = encryptor.TransformFinalBlock(
            Encoding.UTF8.GetBytes(json), 0, json.Length);
        
        // Return IV + encrypted data
        return Convert.ToBase64String(aes.IV.Concat(encrypted).ToArray());
    }
    
    public CookieFile Decrypt(string encrypted)
    {
        var data = Convert.FromBase64String(encrypted);
        
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = data.Take(16).ToArray(); // First 16 bytes are IV
        
        using var decryptor = aes.CreateDecryptor();
        var decrypted = decryptor.TransformFinalBlock(
            data, 16, data.Length - 16);
        
        var json = Encoding.UTF8.GetString(decrypted);
        return JsonSerializer.Deserialize<CookieFile>(json)!;
    }
}
```

## Testing the Integration

### Test Cookie Submission

```bash
# Using curl
curl -X POST https://localhost:7000/api/scraper/cookies \
  -H "Content-Type: application/json" \
  -d @shein_cookies.json

# Using GraphQL
mutation {
  submitCookiesForScraping(input: {
    supplierId: "12345678-1234-1234-1234-123456789012"
    domain: "shein.com"
    cookies: [
      { name: "session", value: "abc123", domain: ".shein.com" }
    ]
  }) {
    success
    scraperSessionId
    message
  }
}
```

### Check Session Status

```bash
# REST
curl https://localhost:7000/api/scraper/sessions/{sessionId}/status

# GraphQL
query {
  getScraperSession(sessionId: "...") {
    id
    status
    startedAt
    completedAt
    ordersScraped
  }
}
```

## Next Steps

1. Choose and implement cookie storage solution
2. Add GraphQL mutations or REST endpoints
3. Implement background service for processing
4. Add error handling and retry logic
5. Set up monitoring and alerting
6. Add rate limiting to prevent abuse
7. Implement cookie refresh workflow

## Related Files

- Scraper implementation: `src/MyMarketManager.Data/Services/Scraping/SheinScraper.cs`
- Scraper interface: `src/MyMarketManager.Data/Services/Scraping/IWebScraper.cs`
- Entity definitions: `src/MyMarketManager.Data/Entities/`
- Architecture docs: `docs/web-scraping.md`
