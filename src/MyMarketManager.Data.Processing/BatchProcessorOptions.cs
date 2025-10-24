using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Processing;

/// <summary>
/// Configuration options for batch processors.
/// </summary>
internal class BatchProcessorOptions
{
    /// <summary>
    /// Dictionary of registered processors keyed by processor name.
    /// Value contains the batch type and processor type.
    /// </summary>
    public Dictionary<string, (StagingBatchType BatchType, Type ProcessorType)> Processors { get; } = new();
}