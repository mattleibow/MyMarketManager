using MyMarketManager.Data.Entities;

namespace MyMarketManager.Data.Processing;

/// <summary>
/// Adapter that wraps a StagingBatch to make it compatible with IWorkItem.
/// This allows StagingBatch to work with the generic work item processing framework
/// without modifying the entity itself.
/// </summary>
public class StagingBatchWorkItem : IWorkItem
{
    private readonly StagingBatch _batch;

    public StagingBatchWorkItem(StagingBatch batch)
    {
        _batch = batch ?? throw new ArgumentNullException(nameof(batch));
    }

    /// <summary>
    /// Gets the underlying StagingBatch.
    /// </summary>
    public StagingBatch Batch => _batch;

    /// <inheritdoc/>
    public Guid Id => _batch.Id;

    /// <inheritdoc/>
    public string? ProcessorName => _batch.BatchProcessorName;
}
