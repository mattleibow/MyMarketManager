namespace MyMarketManager.Data.Processing;

/// <summary>
/// Processes work items of a specific type.
/// </summary>
/// <typeparam name="TWorkItem">The type of work item this processor handles.</typeparam>
public interface IWorkItemProcessor<in TWorkItem> where TWorkItem : IWorkItem
{
    /// <summary>
    /// Processes a single work item.
    /// </summary>
    /// <param name="workItem">The work item to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProcessAsync(TWorkItem workItem, CancellationToken cancellationToken);
}
