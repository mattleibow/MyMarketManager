using MyMarketManager.Data.Entities;

namespace MyMarketManager.Data.Processing;

/// <summary>
/// Work item that wraps a StagingBatch for processing.
/// </summary>
public class StagingBatchWorkItem : IWorkItem
{
    public StagingBatchWorkItem(StagingBatch batch)
    {
        Batch = batch ?? throw new ArgumentNullException(nameof(batch));
    }

    public Guid Id => Batch.Id;

    public StagingBatch Batch { get; }
}
