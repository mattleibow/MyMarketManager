namespace MyMarketManager.Processing;

/// <summary>
/// Configuration options for the WorkItemProcessingService.
/// </summary>
public class WorkItemProcessingServiceOptions
{
    internal List<WorkItemHandlerRegistration> Registrations { get; } = new();

    internal void Register<THandler>(string name, int maxItemsPerCycle, WorkItemHandlerPurpose purpose)
        where THandler : class, IWorkItemHandler
    {
        Registrations.Add(new WorkItemHandlerRegistration(
            typeof(THandler),
            name,
            maxItemsPerCycle,
            purpose));
    }
}
