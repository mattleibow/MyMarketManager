using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMarketManager.Data;
using MyMarketManager.Data.Enums;
using MyMarketManager.Scrapers.Shein;

namespace MyMarketManager.Processing.Handlers;

/// <summary>
/// Handler that fetches and processes Shein staging batches.
/// This handler specifically handles batches with BatchProcessorName = "Shein".
/// </summary>
public class SheinBatchHandler(
    MyMarketManagerDbContext context,
    SheinWebScraper scraper,
    ILogger<SheinBatchHandler> logger) : IWorkItemHandler<WorkItem>
{
    public async Task<IReadOnlyCollection<WorkItem>> FetchNextAsync(int maxItems, CancellationToken cancellationToken)
    {
        // Fetch queued Shein batches from database (no tracking - just IDs)
        var queuedBatchIds = await context.StagingBatches
            .AsNoTracking()
            .Where(b => b.Status == ProcessingStatus.Queued && b.BatchProcessorName == "Shein")
            .OrderBy(b => b.StartedAt)
            .Take(maxItems)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

        logger.LogDebug("Found {Count} queued Shein staging batches", queuedBatchIds.Count);

        return queuedBatchIds.Select(id => new WorkItem(id)).ToList();
    }

    public async Task ProcessAsync(WorkItem workItem, CancellationToken cancellationToken)
    {
        // Reload the batch from the context to ensure it's tracked in this scope's DbContext
        var batch = await context.StagingBatches
            .Include(b => b.Supplier)
            .FirstOrDefaultAsync(b => b.Id == workItem.Id, cancellationToken);

        if (batch == null)
        {
            logger.LogWarning("Batch {BatchId} not found, skipping", workItem.Id);
            return;
        }

        try
        {
            logger.LogInformation("Processing Shein batch {BatchId}", batch.Id);

            await scraper.ProcessBatchAsync(batch, cancellationToken);

            // Mark as complete
            batch.Status = ProcessingStatus.Completed;
            batch.CompletedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully processed Shein batch {BatchId}", batch.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Shein batch {BatchId}", batch.Id);

            batch.Status = ProcessingStatus.Failed;
            batch.ErrorMessage = ex.Message;

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
