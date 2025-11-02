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
}
