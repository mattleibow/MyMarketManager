using Microsoft.Extensions.DependencyInjection;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Processing;

internal sealed class BatchProcessorFactoryBuilder(IServiceCollection services) : IBatchProcessorFactoryBuilder
{
    public IServiceCollection Services { get; } = services;

    /// <summary>
    /// Registers a processor for a specific batch type and name.
    /// </summary>
    public IBatchProcessorFactoryBuilder AddProcessor<TProcessor>(StagingBatchType batchType, string processorName)
        where TProcessor : class, IBatchProcessor
    {
        Services.AddScoped<TProcessor>();

        // Register with options pattern
        Services.Configure<BatchProcessorOptions>(options =>
        {
            options.Processors[processorName] = (batchType, typeof(TProcessor));
        });

        return this;
    }
}
