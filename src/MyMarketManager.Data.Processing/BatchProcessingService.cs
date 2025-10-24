using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMarketManager.Data;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
using MyMarketManager.Data.Processing;

namespace MyMarketManager.WebApp.Services;

public class BatchProcessingService(MyMarketManagerDbContext context, IBatchProcessorFactory factory, ILogger<BatchProcessingService> logger)
{
    public async Task ProcessBatchesAsync(CancellationToken cancellationToken)
    {
        // Get all queued batches
        var queuedBatches = await context.StagingBatches
            .Where(b => b.Status == ProcessingStatus.Queued)
            .Include(b => b.Supplier)
            .ToListAsync(cancellationToken);

        // Order by StartedAt in memory since SQLite doesn't support DateTimeOffset ordering
        queuedBatches = queuedBatches.OrderBy(b => b.StartedAt).ToList();

        logger.LogInformation("Found {Count} queued batches to process", queuedBatches.Count);

        if (queuedBatches.Count == 0)
        {
            return;
        }

        foreach (var batch in queuedBatches)
        {
            await ProcessBatchAsync(batch, cancellationToken);
        }
    }

    private async Task<bool> ProcessBatchAsync(StagingBatch batch, CancellationToken cancellationToken)
    {
        try
        {
            // Skip batches without processor name
            if (string.IsNullOrEmpty(batch.BatchProcessorName))
            {
                logger.LogWarning("Batch {BatchId} has no processor name", batch.Id);
                return false;
            }

            // Get the processor
            var processor = factory.GetProcessor(batch.BatchProcessorName);
            if (processor is null)
            {
                logger.LogWarning(
                    "No processor found for batch {BatchId} - Type: {BatchType}, Name: {ProcessorName}",
                    batch.Id,
                    batch.BatchType,
                    batch.BatchProcessorName);
                return false;
            }

            // Let the processor handle everything
            logger.LogInformation(
                "Starting processor for batch {BatchId} - Type: {BatchType}, Name: {ProcessorName}",
                batch.Id,
                batch.BatchType,
                batch.BatchProcessorName);

            await processor.ProcessBatchAsync(batch, cancellationToken);

            // Mark as complete
            batch.Status = ProcessingStatus.Completed;
            batch.CompletedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing batch {BatchId}", batch.Id);

            batch.Status = ProcessingStatus.Failed;
            batch.ErrorMessage = ex.Message;

            await context.SaveChangesAsync(cancellationToken);

            return false;
        }
    }
}
