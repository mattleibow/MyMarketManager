using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Processing;

public interface IBatchProcessorFactory
{
    IEnumerable<string> GetAvailableProcessors(StagingBatchType batchType);

    IBatchProcessor? GetProcessor(string processorName);
}
