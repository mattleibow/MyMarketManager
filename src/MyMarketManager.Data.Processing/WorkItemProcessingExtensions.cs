using Microsoft.Extensions.DependencyInjection;

namespace MyMarketManager.Data.Processing;

/// <summary>
/// Extension methods for registering work item handlers and the processing engine.
/// </summary>
public static class WorkItemProcessingExtensions
{
    /// <summary>
    /// Adds the work item processing engine and returns a builder for registering handlers.
    /// </summary>
    public static IWorkItemProcessingBuilder AddWorkItemProcessing(this IServiceCollection services)
    {
        // Register the engine as singleton (shared across all scopes)
        services.AddSingleton<WorkItemProcessingEngine>();

        return new WorkItemProcessingBuilder(services);
    }

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

    private class WorkItemProcessingBuilder : IWorkItemProcessingBuilder
    {
        public IServiceCollection Services { get; }

        public WorkItemProcessingBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IWorkItemProcessingBuilder AddHandler<THandler, TWorkItem>(
            string name,
            int maxItemsPerCycle = 10,
            ProcessorPurpose purpose = ProcessorPurpose.Internal)
            where THandler : class, IWorkItemHandler<TWorkItem>
            where TWorkItem : IWorkItem
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(name);
            ArgumentOutOfRangeException.ThrowIfLessThan(maxItemsPerCycle, 1);

            // Register the handler
            Services.AddScoped<THandler>();

            // Register handler configuration using IOptions pattern
            Services.Configure<WorkItemProcessingEngineOptions>(options =>
            {
                options.Registrations.Add(new WorkItemProcessingEngineOptions.HandlerRegistration(
                    typeof(THandler),
                    typeof(TWorkItem),
                    name,
                    maxItemsPerCycle,
                    purpose));
            });

            return this;
        }
    }
}

/// <summary>
/// Defines the purpose or category of a processor for UI filtering.
/// </summary>
public enum ProcessorPurpose
{
    /// <summary>
    /// Processors for data ingestion (e.g., web scrapers, file uploads).
    /// Typically shown on ingestion/import pages.
    /// </summary>
    Ingestion = 0,

    /// <summary>
    /// Internal background processors (e.g., vectorization, cleanup).
    /// Not typically shown in user-facing UI.
    /// </summary>
    Internal = 1,

    /// <summary>
    /// Processors for data export or reporting.
    /// </summary>
    Export = 2
}
