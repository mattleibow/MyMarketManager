using Microsoft.Extensions.DependencyInjection;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Processing;

internal sealed class BatchProcessorFactoryBuilder(IServiceCollection services) : IBatchProcessorFactoryBuilder
{
    public IServiceCollection Services { get; } = services;

    /// <summary>
    /// Registers a processor for a specific batch type and name.
    /// </summary>
    public IBatchProcessorFactoryBuilder AddProcessor<TProcessor>(
        StagingBatchType batchType,
        string processorName,
        ProcessorPurpose purpose = ProcessorPurpose.Ingestion,
        string? displayName = null,
        string? description = null)
        where TProcessor : class, IBatchProcessor
    {
        Services.AddScoped<TProcessor>();

        // Register with options pattern
        Services.Configure<BatchProcessorOptions>(options =>
        {
            options.Processors[processorName] = new ProcessorMetadata
            {
                BatchType = batchType,
                ProcessorType = typeof(TProcessor),
                Purpose = purpose,
                DisplayName = displayName,
                Description = description
            };
        });

        return this;
    }
}
