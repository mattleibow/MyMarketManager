using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMarketManager.Data;
using MyMarketManager.Data.Enums;
using MyMarketManager.Processing;

namespace MyMarketManager.Scrapers.Shein;

/// <summary>
/// Handler that fetches and processes Shein staging batches.
/// This handler specifically handles batches with BatchProcessorName = "Shein".
/// </summary>
public class SheinBatchHandler(
    MyMarketManagerDbContext context,
    SheinWebScraper scraper,
    ILogger<SheinBatchHandler> logger) : IWorkItemHandler<SheinWorkItem>
{
    private readonly MyMarketManagerDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly SheinWebScraper _scraper = scraper ?? throw new ArgumentNullException(nameof(scraper));
    private readonly ILogger<SheinBatchHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<IReadOnlyCollection<SheinWorkItem>> FetchNextAsync(int maxItems, CancellationToken cancellationToken)
    {
        // Fetch queued Shein batches from database
        var queuedBatches = await _context.StagingBatches
            .Where(b => b.Status == ProcessingStatus.Queued && b.BatchProcessorName == "Shein")
            .Include(b => b.Supplier)
            .OrderBy(b => b.StartedAt)
            .Take(maxItems)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Found {Count} queued Shein staging batches", queuedBatches.Count);

        return queuedBatches
            .Select(b => new SheinWorkItem(b))
            .ToList();
    }

    public async Task ProcessAsync(SheinWorkItem workItem, CancellationToken cancellationToken)
    {
        var batch = workItem.Batch;

        try
        {
            _logger.LogInformation(
                "Processing Shein batch {BatchId}",
                batch.Id);

            await _scraper.ProcessBatchAsync(batch, cancellationToken);

            // Mark as complete
            batch.Status = ProcessingStatus.Completed;
            batch.CompletedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully processed Shein batch {BatchId}", batch.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Shein batch {BatchId}", batch.Id);

            batch.Status = ProcessingStatus.Failed;
            batch.ErrorMessage = ex.Message;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
