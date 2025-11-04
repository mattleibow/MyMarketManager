# Background Processing System

The background processing system handles asynchronous work like web scraping, data imports, and cleanup tasks. It uses channels for fair scheduling and concurrent processing.

## Quick Start

### 1. Create a Work Item

Work items represent units of work to process. They must implement `IWorkItem`:

```csharp
public class MyWorkItem : IWorkItem
{
    public Guid Id { get; }
    public string Data { get; set; }
}
```

### 2. Create a Handler

Handlers fetch and process work items. Implement `IWorkItemHandler<TWorkItem>`:

```csharp
public class MyHandler : IWorkItemHandler<MyWorkItem>
{
    public async Task<IReadOnlyCollection<MyWorkItem>> FetchNextAsync(
        int maxItems, CancellationToken cancellationToken)
    {
        // Query your data source and return up to maxItems
        return await GetPendingWorkAsync(maxItems);
    }

    public async Task ProcessAsync(
        MyWorkItem workItem, CancellationToken cancellationToken)
    {
        // Process the work item
        await DoWorkAsync(workItem);
    }
}
```

### 3. Register the Handler

In `Program.cs`, register your handler:

```csharp
builder.Services.AddBackgroundProcessing(config)
    .AddHandler<MyHandler>(
        name: "MyProcessor",
        maxItemsPerCycle: 10,
        purpose: WorkItemHandlerPurpose.Internal);
```

Done! Your handler will now run automatically on the configured poll interval.

## Architecture

The system has three main parts:

1. **WorkItemProcessingService** - Fetches work from all handlers, queues items in a channel, and processes them concurrently
2. **BackgroundProcessingService** - Runs on a timer, calling the processing service each cycle
3. **Your Handlers** - Fetch and process your specific work items

**Flow:**
```
Timer triggers → Fetch from all handlers in parallel → Queue in channel → Process concurrently
```

**Key features:**
- Single background service handles all work types
- Fair scheduling - no handler monopolizes resources
- Bounded channels prevent memory issues
- Handlers are isolated - errors don't affect others

## Configuration

Configure via `appsettings.json`:

```json
{
  "BackgroundProcessing": {
    "PollInterval": "00:05:00"
  }
}
```

Or in code:

```csharp
builder.Services.AddBackgroundProcessing(options =>
{
    options.PollInterval = TimeSpan.FromMinutes(5);
});
```

## Handler Purposes

Categorize handlers for UI filtering:

- **Ingestion** - Import data from external sources (web scraping, API imports)
- **Internal** - Internal processing (cleanup, calculations, aggregations)
- **Export** - Send data to external systems (exports, notifications)

## Best Practices

### Idempotency
Make handlers idempotent - they should be safe to run multiple times on the same work item.

### Error Handling
Catch exceptions in `ProcessAsync` and update work item status. Unhandled exceptions are logged but don't stop other items.

### Batch Sizes
Choose `maxItemsPerCycle` based on:
- Item processing time
- System resources
- Desired throughput

### Database Contexts
Use scoped dependencies. The framework creates a new scope for each handler instance.

### Logging
Use structured logging with work item IDs for traceability.

## Example: Shein Batch Handler

See `SheinBatchHandler` for a complete real-world example that:
- Fetches queued staging batches from database
- Processes each batch using a web scraper
- Updates batch status on completion or failure
- Logs progress and errors

## GraphQL Integration

Get available handlers by purpose:

```csharp
public IEnumerable<string> GetAvailableScrapers(
    [Service] WorkItemProcessingService processingService)
{
    return processingService.GetHandlers(WorkItemHandlerPurpose.Ingestion);
}
```

## Testing

Test handlers independently:

```csharp
[Fact]
public async Task FetchNextAsync_ReturnsPendingItems()
{
    var handler = new MyHandler(...);
    var items = await handler.FetchNextAsync(10, CancellationToken.None);
    Assert.NotEmpty(items);
}
```

No need to test the framework - focus on your handler logic.

## Migration from Old System

The new system replaces:
- Individual `BackgroundService` implementations → Single `BackgroundProcessingService`
- Manual service registration → Fluent `AddHandler()` API
- `IBatchProcessor` interface → `IWorkItemHandler<T>` interface

Key changes:
- Method renamed: `FetchWorkItemsAsync` → `FetchNextAsync`
- Registration simplified: Single call with name and config
- No factory pattern needed
