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
        /// Registers a work item handler.
        /// </summary>
        IWorkItemProcessingBuilder AddHandler<THandler, TWorkItem>()
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

        public IWorkItemProcessingBuilder AddHandler<THandler, TWorkItem>()
            where THandler : class, IWorkItemHandler<TWorkItem>
            where TWorkItem : IWorkItem
        {
            // Register the handler
            Services.AddScoped<THandler>();

            // Register it with the engine
            Services.AddSingleton<IConfigureWorkItemProcessingEngine>(sp =>
                new ConfigureWorkItemProcessingEngine<TWorkItem>(typeof(THandler)));

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

        public ConfigureWorkItemProcessingEngine(Type handlerType)
        {
            _handlerType = handlerType;
        }

        public void Configure(WorkItemProcessingEngine engine)
        {
            engine.RegisterHandler<TWorkItem>(_handlerType);
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
