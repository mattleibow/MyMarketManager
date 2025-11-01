using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Processing;

public interface IBatchProcessorFactory
{
    IEnumerable<string> GetAvailableProcessors(StagingBatchType batchType);

    IEnumerable<string> GetAvailableProcessors(StagingBatchType batchType, ProcessorPurpose purpose);

    IEnumerable<string> GetProcessorsByPurpose(ProcessorPurpose purpose);

    ProcessorMetadata? GetProcessorMetadata(string processorName);

    IBatchProcessor? GetProcessor(string processorName);
}
