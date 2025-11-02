namespace MyMarketManager.Data.Processing;

/// <summary>
/// Represents a unit of work to be processed.
/// Work items are fetched by IWorkItemSource implementations and queued for processing.
/// </summary>
public interface IWorkItem
{
    /// <summary>
    /// Unique identifier for this work item.
    /// </summary>
    Guid Id { get; }
}
