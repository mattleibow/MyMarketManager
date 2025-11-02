using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMarketManager.Data;
using MyMarketManager.Data.Enums;
using MyMarketManager.Data.Processing;
using MyMarketManager.Scrapers.Shein;

namespace MyMarketManager.WebApp.Services;

/// <summary>
/// Handler that fetches and processes Shein staging batches.
/// This handler specifically handles batches with BatchProcessorName = "Shein".
/// </summary>
public class SheinBatchHandler : IWorkItemHandler<StagingBatchWorkItem>
{
    private readonly MyMarketManagerDbContext _context;
    private readonly SheinWebScraper _scraper;
    private readonly ILogger<SheinBatchHandler> _logger;

    public SheinBatchHandler(
        MyMarketManagerDbContext context,
        SheinWebScraper scraper,
        ILogger<SheinBatchHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _scraper = scraper ?? throw new ArgumentNullException(nameof(scraper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyCollection<StagingBatchWorkItem>> FetchWorkItemsAsync(
        int maxItems, 
        CancellationToken cancellationToken)
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
            .Select(b => new StagingBatchWorkItem(b))
            .ToList();
    }

    public async Task ProcessAsync(StagingBatchWorkItem workItem, CancellationToken cancellationToken)
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
