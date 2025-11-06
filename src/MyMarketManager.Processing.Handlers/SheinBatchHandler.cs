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
    ILogger<SheinBatchHandler> logger) : IWorkItemHandler<SheinWorkItem>
{
    public async Task<IReadOnlyCollection<SheinWorkItem>> FetchNextAsync(int maxItems, CancellationToken cancellationToken)
    {
        // Fetch queued Shein batches from database
        var queuedBatches = await context.StagingBatches
            .Where(b => b.Status == ProcessingStatus.Queued && b.BatchProcessorName == "Shein")
            .Include(b => b.Supplier)
            .OrderBy(b => b.StartedAt)
            .Take(maxItems)
            .ToListAsync(cancellationToken);

        logger.LogDebug("Found {Count} queued Shein staging batches", queuedBatches.Count);

        return [.. queuedBatches.Select(b => new SheinWorkItem(b))];
    }

    public async Task ProcessAsync(SheinWorkItem workItem, CancellationToken cancellationToken)
    {
        // Entity is already tracked in this scope's DbContext from FetchNextAsync
        var batch = workItem.Batch;

        try
        {
            logger.LogInformation(
                "Processing Shein batch {BatchId}",
                batch.Id);

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
