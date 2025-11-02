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

## Migration from Old System

The old services have been removed:
- ❌ `IngestionService` → ✅ `SheinBatchHandler`
- ❌ `BatchProcessingService` → ✅ Handler-based architecture

All functionality is preserved, just better organized with the new handler-based system.

## See Also

- [Web Scraping](web-scraping.md) - Web scraping infrastructure details
- [Architecture](architecture.md) - System architecture overview
- [Development Guide](development-guide.md) - Development workflows
