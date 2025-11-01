using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMarketManager.Data;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Processing;

/// <summary>
/// Handler that fetches and processes queued staging batches.
/// This replaces the old BatchProcessingService logic.
/// </summary>
public class StagingBatchHandler : IWorkItemHandler<StagingBatchWorkItem>
{
    private readonly MyMarketManagerDbContext _context;
    private readonly IBatchProcessorFactory _factory;
    private readonly ILogger<StagingBatchHandler> _logger;

    public StagingBatchHandler(
        MyMarketManagerDbContext context,
        IBatchProcessorFactory factory,
        ILogger<StagingBatchHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Name => "StagingBatchProcessor";

    public int MaxItemsPerCycle => 5; // Process up to 5 batches per cycle

    public async Task<IReadOnlyCollection<StagingBatchWorkItem>> FetchWorkItemsAsync(int maxItems, CancellationToken cancellationToken)
    {
        // Fetch queued batches from database
        var queuedBatches = await _context.StagingBatches
            .Where(b => b.Status == ProcessingStatus.Queued)
            .Include(b => b.Supplier)
            .OrderBy(b => b.StartedAt)
            .Take(maxItems)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Found {Count} queued staging batches", queuedBatches.Count);

        return queuedBatches
            .Select(b => new StagingBatchWorkItem(b))
            .ToList();
    }

    public async Task ProcessAsync(StagingBatchWorkItem workItem, CancellationToken cancellationToken)
    {
        var batch = workItem.Batch;

        try
        {
            // Skip batches without processor name
            if (string.IsNullOrEmpty(batch.BatchProcessorName))
            {
                _logger.LogWarning("Batch {BatchId} has no processor name", batch.Id);
                return;
            }

            // Get the processor
            var processor = _factory.GetProcessor(batch.BatchProcessorName);
            if (processor is null)
            {
                _logger.LogWarning(
                    "No processor found for batch {BatchId} - Type: {BatchType}, Name: {ProcessorName}",
                    batch.Id,
                    batch.BatchType,
                    batch.BatchProcessorName);
                return;
            }

            // Let the processor handle everything
            _logger.LogInformation(
                "Processing batch {BatchId} - Type: {BatchType}, Name: {ProcessorName}",
                batch.Id,
                batch.BatchType,
                batch.BatchProcessorName);

            await processor.ProcessBatchAsync(batch, cancellationToken);

            // Mark as complete
            batch.Status = ProcessingStatus.Completed;
            batch.CompletedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully processed batch {BatchId}", batch.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing batch {BatchId}", batch.Id);

            batch.Status = ProcessingStatus.Failed;
            batch.ErrorMessage = ex.Message;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
