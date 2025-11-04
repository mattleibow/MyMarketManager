namespace MyMarketManager.Processing;

/// <summary>
/// Non-generic base interface for work item handlers.
/// This is used internally by the processing service to work with handlers without knowing their specific types.
/// </summary>
public interface IWorkItemHandler
{
    /// <summary>
    /// Fetches the next batch of work items to process.
    /// </summary>
    /// <param name="maxItems">Maximum number of items to fetch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of work items to process.</returns>
    Task<IReadOnlyCollection<IWorkItem>> FetchNextAsync(int maxItems, CancellationToken cancellationToken);

    /// <summary>
    /// Processes a single work item.
    /// </summary>
    /// <param name="workItem">The work item to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProcessAsync(IWorkItem workItem, CancellationToken cancellationToken);
}
