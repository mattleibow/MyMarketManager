using Microsoft.Extensions.DependencyInjection;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Processing;

public interface IBatchProcessorFactoryBuilder
{
    IServiceCollection Services { get; }

    IBatchProcessorFactoryBuilder AddProcessor<TProcessor>(
        StagingBatchType batchType, 
        string processorName,
        ProcessorPurpose purpose = ProcessorPurpose.Ingestion,
        string? displayName = null,
        string? description = null)
        where TProcessor : class, IBatchProcessor;
}
