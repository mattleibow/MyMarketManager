using MyMarketManager.Data.Entities;

namespace MyMarketManager.Data.Processing;

/// <summary>
/// Adapter that allows existing IBatchProcessor implementations to work with the generic
/// IWorkItemProcessor framework without modification.
/// </summary>
public class BatchProcessorAdapter : IWorkItemProcessor<StagingBatchWorkItem>
{
    private readonly IBatchProcessor _batchProcessor;

    public BatchProcessorAdapter(IBatchProcessor batchProcessor)
    {
        _batchProcessor = batchProcessor ?? throw new ArgumentNullException(nameof(batchProcessor));
    }

    public Task ProcessAsync(StagingBatchWorkItem workItem, CancellationToken cancellationToken)
    {
        return _batchProcessor.ProcessBatchAsync(workItem.Batch, cancellationToken);
    }
}
