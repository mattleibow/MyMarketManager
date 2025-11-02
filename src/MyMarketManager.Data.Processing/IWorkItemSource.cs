namespace MyMarketManager.Data.Processing;

/// <summary>
/// Defines a source that can fetch work items to be processed.
/// Each processor implementation knows how to query its own data source.
/// </summary>
/// <typeparam name="TWorkItem">The type of work item this source produces.</typeparam>
public interface IWorkItemSource<TWorkItem> where TWorkItem : IWorkItem
{
    /// <summary>
    /// Fetches the next batch of work items to process.
    /// </summary>
    /// <param name="maxItems">Maximum number of items to fetch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of work items to process.</returns>
    Task<IReadOnlyCollection<TWorkItem>> FetchWorkItemsAsync(int maxItems, CancellationToken cancellationToken);
}
