using MyMarketManager.Data.Entities;
using MyMarketManager.Processing;

namespace MyMarketManager.Data.Processing;

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
