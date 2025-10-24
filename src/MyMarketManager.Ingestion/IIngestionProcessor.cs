using MyMarketManager.Data.Entities;

namespace MyMarketManager.Ingestion;

/// <summary>
/// Interface for batch ingestion processors.
/// Each processor is responsible for processing a specific type of batch.
/// </summary>
public interface IIngestionProcessor
{
    /// <summary>
    /// Determines if this processor can handle the given batch.
    /// </summary>
    /// <param name="batch">The batch to check.</param>
    /// <returns>True if this processor can handle the batch, false otherwise.</returns>
    bool CanProcess(StagingBatch batch);

    /// <summary>
    /// Processes a single staging batch.
    /// </summary>
    /// <param name="batch">The batch to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProcessBatchAsync(StagingBatch batch, CancellationToken cancellationToken = default);
}
