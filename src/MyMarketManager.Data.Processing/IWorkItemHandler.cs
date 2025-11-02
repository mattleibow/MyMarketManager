namespace MyMarketManager.Data.Processing;

/// <summary>
/// Combined interface for processors that both fetch and process work items.
/// This is the primary interface that processors implement.
/// Name and MaxItemsPerCycle are specified during registration, not by the handler itself.
/// </summary>
/// <typeparam name="TWorkItem">The type of work item this handler manages.</typeparam>
public interface IWorkItemHandler<TWorkItem> : IWorkItemSource<TWorkItem>, IWorkItemProcessor<TWorkItem> 
    where TWorkItem : IWorkItem
{
}
