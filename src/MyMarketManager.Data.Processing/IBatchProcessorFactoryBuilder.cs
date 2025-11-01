using Microsoft.Extensions.DependencyInjection;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Processing;

public interface IBatchProcessorFactoryBuilder
{
    IServiceCollection Services { get; }

    IBatchProcessorFactoryBuilder AddProcessor<TProcessor>(StagingBatchType batchType, string processorName)
        where TProcessor : class, IBatchProcessor;
}
