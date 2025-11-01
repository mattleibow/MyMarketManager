using Microsoft.Extensions.DependencyInjection;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Processing;

internal sealed class BatchProcessorFactoryBuilder(IServiceCollection services) : IBatchProcessorFactoryBuilder
{
    public IServiceCollection Services { get; } = services;

    /// <summary>
    /// Registers a batch processor for a specific batch type and name.
    /// This wraps the IBatchProcessor in an adapter to work with the generic framework.
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
                WorkItemType = typeof(StagingBatchWorkItem),
                ProcessorType = typeof(TProcessor),
                Purpose = purpose,
                DisplayName = displayName,
                Description = description
            };
        });

        return this;
    }

    /// <summary>
    /// Registers a generic work item processor.
    /// </summary>
    public IBatchProcessorFactoryBuilder AddWorkItemProcessor<TProcessor, TWorkItem>(
        string processorName,
        ProcessorPurpose purpose = ProcessorPurpose.Internal,
        string? displayName = null,
        string? description = null)
        where TProcessor : class, IWorkItemProcessor<TWorkItem>
        where TWorkItem : IWorkItem
    {
        Services.AddScoped<TProcessor>();

        // Register with options pattern
        Services.Configure<BatchProcessorOptions>(options =>
        {
            options.Processors[processorName] = new ProcessorMetadata
            {
                BatchType = null, // Not a StagingBatch processor
                WorkItemType = typeof(TWorkItem),
                ProcessorType = typeof(TProcessor),
                Purpose = purpose,
                DisplayName = displayName,
                Description = description
            };
        });

        return this;
    }
}

