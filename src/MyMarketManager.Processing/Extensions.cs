using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MyMarketManager.Processing;

/// <summary>
/// Extension methods for registering work item handlers and the processing engine.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds the work item processing engine and returns a builder for registering handlers.
    /// </summary>
    public static IBackgroundProcessingBuilder AddBackgroundProcessing(this IServiceCollection services)
    {
        // Register the engine as singleton (shared across all scopes)
        services.AddSingleton<WorkItemProcessingService>();

        // Register the unified background processing service
        services.AddHostedService<BackgroundProcessingService>();

        return new BackgroundProcessingBuilder(services);
    }

    /// <summary>
    /// Adds the work item processing engine and returns a builder for registering handlers.
    /// </summary>
    public static IBackgroundProcessingBuilder AddBackgroundProcessing(this IServiceCollection services, IConfiguration config)
    {
        // Configure background processing options from provided configuration
        services.Configure<BackgroundProcessingOptions>(config);

        return services.AddBackgroundProcessing();
    }

    /// <summary>
    /// Adds the work item processing engine and returns a builder for registering handlers.
    /// </summary>
    public static IBackgroundProcessingBuilder AddBackgroundProcessing(this IServiceCollection services, Action<BackgroundProcessingOptions> configureOptions)
    {
        // Configure background processing options from provided configuration
        services.Configure<BackgroundProcessingOptions>(configureOptions);

        return services.AddBackgroundProcessing();
    }
}
