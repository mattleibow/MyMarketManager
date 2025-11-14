namespace MyMarketManager.Processing;

/// <summary>
/// Simple work item that contains just an ID.
/// Used for work items that only need to look up data by ID.
/// </summary>
public class WorkItem(Guid id) : IWorkItem
{
    public Guid Id { get; } = id;
}
