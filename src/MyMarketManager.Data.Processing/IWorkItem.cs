namespace MyMarketManager.Data.Processing;

/// <summary>
/// Represents a unit of work that can be processed by a batch processor.
/// This is a marker interface to identify work items in the system.
/// </summary>
public interface IWorkItem
{
    /// <summary>
    /// Unique identifier for this work item.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// The name of the processor that should handle this work item.
    /// </summary>
    string? ProcessorName { get; }
}
