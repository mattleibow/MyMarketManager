# Background Processing System

The background processing system handles asynchronous work items like web scraping, image vectorization, and data cleanup. It uses a Channel-based architecture for fair scheduling, starvation prevention, and concurrent processing.

## Overview

The system is built on Microsoft's `System.Threading.Channels` pattern with a single `BackgroundService` orchestrating multiple work item handlers. Each handler fetches its own work items and processes them independently.

**Key Benefits:**
- Single background service replaces multiple independent services
- Fair scheduling prevents any handler from monopolizing resources
- Bounded channels prevent memory issues
- Concurrent processing with backpressure control
- Easy to add new processors without creating new services

## Architecture

```
Registration (Program.cs)
    ↓
WorkItemProcessingEngine
    ↓ Fetch Phase (parallel)
    ├─> Handler A: FetchWorkItemsAsync(maxItems)
    ├─> Handler B: FetchWorkItemsAsync(maxItems)
    └─> Handler C: FetchWorkItemsAsync(maxItems)
    ↓
Channel<WorkItemEnvelope> (bounded queue)
    ↓ Process Phase (concurrent)
    ├─> Process item 1
    ├─> Process item 2
    └─> Process item N
```

### Core Components

**`IWorkItem`** - Marker interface for work items
```csharp
public interface IWorkItem
{
    Guid Id { get; }
}
```

**`IWorkItemHandler<TWorkItem>`** - Combined fetcher and processor
```csharp
public interface IWorkItemHandler<TWorkItem> : IWorkItemSource<TWorkItem>, IWorkItemProcessor<TWorkItem>
    where TWorkItem : IWorkItem
{
    // Implementation handles both fetching and processing
}
```

**`WorkItemProcessingEngine`** - Orchestrates the fetch/process cycle
- Manages handler registrations
- Coordinates bounded channel queue
- Validates item counts
- Provides purpose-based filtering

**`UnifiedBackgroundProcessingService`** - Single hosted service
- Polls on configurable interval (default: 5 minutes)
- Calls engine to process one cycle
- Handles errors and continues running

## Creating a New Handler

### Step 1: Define Your Work Item

Create a class implementing `IWorkItem`:

```csharp
public class MyWorkItem : IWorkItem
{
    public Guid Id { get; }
    public string SomeData { get; set; }
    
    public MyWorkItem(Guid id)
    {
        Id = id;
    }
}
```

### Step 2: Implement the Handler

Create a handler implementing `IWorkItemHandler<TWorkItem>`:

```csharp
public class MyWorkItemHandler : IWorkItemHandler<MyWorkItem>
{
    private readonly MyMarketManagerDbContext _context;
    private readonly ILogger<MyWorkItemHandler> _logger;

    public MyWorkItemHandler(
        MyMarketManagerDbContext context,
        ILogger<MyWorkItemHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Fetch work items from your data source
    public async Task<IReadOnlyCollection<MyWorkItem>> FetchWorkItemsAsync(
        int maxItems, 
        CancellationToken cancellationToken)
    {
        // Query your data source
        var items = await _context.MyEntities
            .Where(e => e.NeedsProcessing)
            .Take(maxItems)  // IMPORTANT: Respect the limit
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Found {Count} items to process", items.Count);

        return items
            .Select(e => new MyWorkItem(e.Id) { SomeData = e.Data })
            .ToList();
    }

    // Process a single work item
    public async Task ProcessAsync(
        MyWorkItem workItem, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing item {Id}", workItem.Id);

            // Do your processing work here
            // ...

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process item {Id}", workItem.Id);
            throw;
        }
    }
}
```

### Step 3: Register the Handler

In `Program.cs`, register your handler with configuration:

```csharp
builder.Services.AddWorkItemProcessing()
    .AddHandler<MyWorkItemHandler, MyWorkItem>(
        name: "MyProcessor",           // Unique name
        maxItemsPerCycle: 10,           // Max items per cycle
        purpose: ProcessorPurpose.Internal  // For UI filtering
    );
```

### Registration Parameters

- **`name`** (required) - Unique identifier for this handler registration
  - Used for logging and configuration lookup
  - Allows registering same handler type multiple times with different names
  
- **`maxItemsPerCycle`** (default: 10) - Maximum items to process per cycle
  - Prevents starvation by limiting each handler
  - Engine validates and truncates if handler returns more
  
- **`purpose`** (default: Internal) - Category for UI filtering
  - `Ingestion` - User-facing processors (web scrapers, file uploads)
  - `Internal` - Background processors (vectorization, cleanup)
  - `Export` - Export and reporting processors

## Processor Categories (Purpose)

Use `ProcessorPurpose` to categorize handlers for UI display:

```csharp
public enum ProcessorPurpose
{
    Ingestion = 0,  // Show on ingestion/import pages
    Internal = 1,   // Hide from UI
    Export = 2      // Show on export/reporting pages
}
```

### Querying by Purpose

Get handlers for a specific purpose (useful for UI):

```csharp
var ingestionHandlers = engine.GetHandlerNamesByPurpose(ProcessorPurpose.Ingestion);
// Returns: ["Shein", "Yoco", ...]
```

## Built-in Handlers

### SheinBatchHandler

Processes Shein web scraping batches:

```csharp
.AddHandler<SheinBatchHandler, StagingBatchWorkItem>(
    name: "Shein",
    maxItemsPerCycle: 5,
    purpose: ProcessorPurpose.Ingestion)
```

- Fetches `StagingBatch` records where `BatchProcessorName = "Shein"`
- Delegates to `SheinWebScraper` for processing
- Updates batch status (Completed/Failed)

### ImageVectorizationHandler

Generates vector embeddings for product photos:

```csharp
.AddHandler<ImageVectorizationHandler, ImageVectorizationWorkItem>(
    name: "ImageVectorization",
    maxItemsPerCycle: 10,
    purpose: ProcessorPurpose.Internal)
```

- Fetches `ProductPhoto` records without embeddings
- Generates embeddings using Azure AI
- Stores vectors in database

## Configuration

Configure the polling interval in `appsettings.json`:

```json
{
  "IngestionService": {
    "PollInterval": "00:05:00"  // 5 minutes
  }
}
```

Or configure programmatically:

```csharp
builder.Services.Configure<UnifiedBackgroundProcessingOptions>(options =>
{
    options.PollInterval = TimeSpan.FromMinutes(5);
});
```

## Advanced Scenarios

### Multiple Instances of Same Handler

Register the same handler type multiple times with different configurations:

```csharp
builder.Services.AddWorkItemProcessing()
    // Product photos
    .AddHandler<ImageVectorizationHandler, ImageVectorizationWorkItem>(
        name: "ProductImageVectorization",
        maxItemsPerCycle: 10,
        purpose: ProcessorPurpose.Internal)
    // Delivery photos (future)
    .AddHandler<DeliveryImageVectorizationHandler, ImageVectorizationWorkItem>(
        name: "DeliveryImageVectorization",
        maxItemsPerCycle: 5,
        purpose: ProcessorPurpose.Internal);
```

### Handler with External Dependencies

Inject any service from DI:

```csharp
public class BlobCleanupHandler : IWorkItemHandler<BlobCleanupWorkItem>
{
    private readonly BlobServiceClient _blobClient;
    private readonly ILogger<BlobCleanupHandler> _logger;

    public BlobCleanupHandler(
        BlobServiceClient blobClient,
        ILogger<BlobCleanupHandler> logger)
    {
        _blobClient = blobClient;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<BlobCleanupWorkItem>> FetchWorkItemsAsync(
        int maxItems, 
        CancellationToken cancellationToken)
    {
        // Query blob storage for old files
        var cutoff = DateTimeOffset.UtcNow.AddHours(-24);
        var oldBlobs = new List<BlobCleanupWorkItem>();

        await foreach (var blob in _blobClient
            .GetBlobContainerClient("search-images")
            .GetBlobsAsync(cancellationToken: cancellationToken))
        {
            if (blob.Properties.CreatedOn < cutoff)
            {
                oldBlobs.Add(new BlobCleanupWorkItem(blob.Name, blob.Properties.CreatedOn.Value));
                
                if (oldBlobs.Count >= maxItems)
                    break;
            }
        }

        return oldBlobs;
    }

    public async Task ProcessAsync(BlobCleanupWorkItem workItem, CancellationToken cancellationToken)
    {
        await _blobClient
            .GetBlobContainerClient("search-images")
            .DeleteBlobAsync(workItem.BlobName, cancellationToken: cancellationToken);
        
        _logger.LogInformation("Deleted blob {BlobName} from {CreatedAt}",
            workItem.BlobName, workItem.CreatedAt);
    }
}
```

### Handler with Configuration

Load configuration per handler name:

```csharp
public class ConfigurableHandler : IWorkItemHandler<MyWorkItem>
{
    private readonly MyHandlerOptions _options;

    public ConfigurableHandler(
        IOptionsSnapshot<MyHandlerOptions> options,
        IConfiguration configuration)
    {
        // Load config using handler name
        // Assumes handler registered with name "MyProcessor"
        _options = configuration
            .GetSection("BackgroundProcessing:MyProcessor")
            .Get<MyHandlerOptions>() ?? new();
    }
}
```

In `appsettings.json`:

```json
{
  "BackgroundProcessing": {
    "MyProcessor": {
      "BatchSize": 50,
      "Timeout": "00:01:00"
    }
  }
}
```

## Best Practices

### Respect maxItems Parameter

Always respect the `maxItems` parameter in `FetchWorkItemsAsync`:

```csharp
// ✅ GOOD: Respects limit
var items = await _context.Items
    .Where(i => i.NeedsProcessing)
    .Take(maxItems)
    .ToListAsync(cancellationToken);

// ❌ BAD: Ignores limit
var items = await _context.Items
    .Where(i => i.NeedsProcessing)
    .ToListAsync(cancellationToken);  // Could return thousands!
```

The engine validates and truncates, but violating the limit wastes resources and causes warning logs.

### Handle Errors Gracefully

Wrap processing in try-catch to prevent one failure from stopping the entire cycle:

```csharp
public async Task ProcessAsync(MyWorkItem workItem, CancellationToken cancellationToken)
{
    try
    {
        // Processing logic
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to process {Id}", workItem.Id);
        
        // Mark item as failed in database if applicable
        // Don't re-throw unless you want to stop all processing
    }
}
```

### Use Appropriate Logging Levels

- `LogDebug` - Detailed fetch/process steps
- `LogInformation` - Work item counts, successful completions
- `LogWarning` - Unusual conditions (returned too many items)
- `LogError` - Processing failures

### Keep Processing Fast

The cycle runs sequentially:
1. Fetch from all handlers
2. Process all items
3. Wait for interval
4. Repeat

Long-running processing delays the next cycle. For heavy work:
- Keep `maxItemsPerCycle` reasonable
- Consider reducing poll interval
- Or split into multiple handlers

### Test Your Handler

Create unit tests for fetch and process logic:

```csharp
[Fact]
public async Task FetchWorkItemsAsync_RespectsMaxItems()
{
    // Arrange
    var handler = new MyWorkItemHandler(_context, _logger);
    
    // Add 100 items to database
    // ...
    
    // Act
    var items = await handler.FetchWorkItemsAsync(maxItems: 10, CancellationToken.None);
    
    // Assert
    Assert.Equal(10, items.Count);
}

[Fact]
public async Task ProcessAsync_UpdatesDatabase()
{
    // Arrange
    var handler = new MyWorkItemHandler(_context, _logger);
    var workItem = new MyWorkItem(Guid.NewGuid());
    
    // Act
    await handler.ProcessAsync(workItem, CancellationToken.None);
    
    // Assert
    var entity = await _context.MyEntities.FindAsync(workItem.Id);
    Assert.NotNull(entity);
    Assert.True(entity.IsProcessed);
}
```

## Monitoring and Debugging

### Logging

The engine logs key events:

- Handler registration: `"Registered handler: {Type} as '{Name}'"`
- Fetch phase: `"Handler '{Name}' fetched {Count} work items"`
- Validation: `"Handler '{Name}' returned {Actual} items but max is {Max}. Truncating."`
- Processing: `"Processing {Count} work items across all handlers"`
- Errors: `"Error processing work item {Id} from handler {Name}"`

### Common Issues

**Handler not running:**
- Check registration in `Program.cs`
- Verify `UnifiedBackgroundProcessingService` is registered
- Check logs for initialization messages

**Items not being fetched:**
- Verify database query in `FetchWorkItemsAsync`
- Check `maxItems` is reasonable
- Review fetch phase logs

**Processing errors:**
- Check exception logs
- Verify dependencies are registered in DI
- Test handler logic in isolation

## Migration from Old System

The old services have been removed:
- ❌ `IngestionService` → ✅ `SheinBatchHandler`
- ❌ `ImageVectorizationService` → ✅ `ImageVectorizationHandler`
- ❌ `BatchProcessingService` → ✅ Handler-based architecture

All functionality is preserved, just better organized with the new handler-based system.

## See Also

- [Web Scraping](web-scraping.md) - Web scraping infrastructure details
- [Architecture](architecture.md) - System architecture overview
- [Development Guide](development-guide.md) - Development workflows
