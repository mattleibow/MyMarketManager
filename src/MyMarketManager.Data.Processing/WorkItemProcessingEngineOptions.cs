namespace MyMarketManager.Data.Processing;

/// <summary>
/// Configuration options for the WorkItemProcessingEngine.
/// </summary>
public class WorkItemProcessingEngineOptions
{
    internal List<HandlerRegistration> Registrations { get; } = new();

    internal record HandlerRegistration(
        Type HandlerType,
        Type WorkItemType,
        string Name,
        int MaxItemsPerCycle,
        ProcessorPurpose Purpose);
}
