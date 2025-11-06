using MyMarketManager.Data.Entities;

namespace MyMarketManager.Processing.Handlers;

/// <summary>
/// Work item that wraps a StagingBatch for processing.
/// </summary>
public class SheinWorkItem(StagingBatch batch) : IWorkItem
{
    public Guid Id => Batch.Id;

    public StagingBatch Batch { get; } = batch ?? throw new ArgumentNullException(nameof(batch));
}
