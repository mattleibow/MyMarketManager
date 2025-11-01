namespace MyMarketManager.Data.Processing;

/// <summary>
/// Combined interface for processors that both fetch and process work items.
/// This is the primary interface that processors implement.
/// </summary>
/// <typeparam name="TWorkItem">The type of work item this handler manages.</typeparam>
public interface IWorkItemHandler<TWorkItem> : IWorkItemSource<TWorkItem>, IWorkItemProcessor<TWorkItem> 
    where TWorkItem : IWorkItem
{
    /// <summary>
    /// The unique name of this handler (e.g., "StagingBatchProcessor", "ImageVectorization").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Maximum number of items to process per cycle.
    /// Defaults to 10 if not specified.
    /// </summary>
    int MaxItemsPerCycle => 10;
}
