using MyMarketManager.Data.Entities;

namespace MyMarketManager.Processing.Handlers;

/// <summary>
/// Work item that wraps a StagingBatch for processing.
/// </summary>
public class SheinWorkItem : IWorkItem
{
    public SheinWorkItem(StagingBatch batch)
    {
        Batch = batch ?? throw new ArgumentNullException(nameof(batch));
    }

    public Guid Id => Batch.Id;

    public StagingBatch Batch { get; }
}
