namespace MyMarketManager.Processing;

/// <summary>
/// Combined interface for processors that both fetch and process work items.
/// This is the primary interface that processors implement.
/// Name and MaxItemsPerCycle are specified during registration, not by the handler itself.
/// </summary>
/// <typeparam name="TWorkItem">The type of work item this handler manages.</typeparam>
public interface IWorkItemHandler<TWorkItem> : IWorkItemHandler
    where TWorkItem : IWorkItem
{
    /// <summary>
    /// Fetches the next batch of work items to process.
    /// </summary>
    /// <param name="maxItems">Maximum number of items to fetch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of work items to process.</returns>
    new Task<IReadOnlyCollection<TWorkItem>> FetchNextAsync(int maxItems, CancellationToken cancellationToken);

    /// <summary>
    /// Default implementation of non-generic FetchNextAsync that calls the generic version.
    /// </summary>
    async Task<IReadOnlyCollection<IWorkItem>> IWorkItemHandler.FetchNextAsync(int maxItems, CancellationToken cancellationToken)
    {
        var items = await FetchNextAsync(maxItems, cancellationToken);
        return [.. items];
    }

    /// <summary>
    /// Processes a single work item.
    /// </summary>
    /// <param name="workItem">The work item to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProcessAsync(TWorkItem workItem, CancellationToken cancellationToken);

    /// <summary>
    /// Default implementation of non-generic ProcessAsync that calls the generic version.
    /// </summary>
    Task IWorkItemHandler.ProcessAsync(IWorkItem workItem, CancellationToken cancellationToken) =>
        ProcessAsync((TWorkItem)workItem, cancellationToken);
}
