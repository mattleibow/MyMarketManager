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
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Handler name cannot be null or empty", nameof(name));
            if (maxItemsPerCycle < 1)
                throw new ArgumentException("Max items per cycle must be at least 1", nameof(maxItemsPerCycle));

            // Register the handler
            Services.AddScoped<THandler>();

            // Register it with the engine
            Services.AddSingleton<IConfigureWorkItemProcessingEngine>(sp =>
                new ConfigureWorkItemProcessingEngine<TWorkItem>(
                    typeof(THandler), 
                    name, 
                    maxItemsPerCycle, 
                    purpose));

            return this;
        }
    }

    // Helper interface for configuration
    private interface IConfigureWorkItemProcessingEngine
    {
        void Configure(WorkItemProcessingEngine engine);
    }

    private class ConfigureWorkItemProcessingEngine<TWorkItem> : IConfigureWorkItemProcessingEngine 
        where TWorkItem : IWorkItem
    {
        private readonly Type _handlerType;
        private readonly string _name;
        private readonly int _maxItemsPerCycle;
        private readonly ProcessorPurpose _purpose;

        public ConfigureWorkItemProcessingEngine(
            Type handlerType, 
            string name, 
            int maxItemsPerCycle, 
            ProcessorPurpose purpose)
        {
            _handlerType = handlerType;
            _name = name;
            _maxItemsPerCycle = maxItemsPerCycle;
            _purpose = purpose;
        }

        public void Configure(WorkItemProcessingEngine engine)
        {
            engine.RegisterHandler<TWorkItem>(_handlerType, _name, _maxItemsPerCycle, _purpose);
        }
    }

    // Called internally to configure the engine with all registered handlers
    internal static void ConfigureEngine(IServiceProvider serviceProvider, WorkItemProcessingEngine engine)
    {
        var configurators = serviceProvider.GetServices<IConfigureWorkItemProcessingEngine>();
        foreach (var configurator in configurators)
        {
            configurator.Configure(engine);
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
