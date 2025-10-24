using MyMarketManager.Data.Entities;

namespace MyMarketManager.Scrapers;

/// <summary>
/// Interface for batch processors that handle staging batches.
/// </summary>
public interface IBatchProcessor
{
    /// <summary>
    /// Processes a staging batch.
    /// </summary>
    /// <param name="batch">The batch to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProcessBatchAsync(StagingBatch batch, CancellationToken cancellationToken);
}
