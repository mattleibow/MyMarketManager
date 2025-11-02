using Microsoft.Extensions.DependencyInjection;

namespace MyMarketManager.Data.Processing;

/// <summary>
/// Builder for registering work item handlers.
/// </summary>
public interface IWorkItemProcessingBuilder
{
    IServiceCollection Services { get; }

    /// <summary>
    /// Registers a work item handler with a name and max items per cycle.
    /// </summary>
    /// <param name="name">Unique name for this handler registration (e.g., "Shein", "ImageVectorization")</param>
    /// <param name="maxItemsPerCycle">Maximum number of items to process per cycle (default: 10)</param>
    /// <param name="purpose">Purpose/category of this handler for UI filtering (default: Internal)</param>
    IWorkItemProcessingBuilder AddHandler<THandler, TWorkItem>(
        string name,
        int maxItemsPerCycle = 10,
        ProcessorPurpose purpose = ProcessorPurpose.Internal)
        where THandler : class, IWorkItemHandler<TWorkItem>
        where TWorkItem : IWorkItem;
}
