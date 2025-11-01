namespace MyMarketManager.Data.Processing;

/// <summary>
/// Configuration options for batch processors.
/// </summary>
internal class BatchProcessorOptions
{
    /// <summary>
    /// Dictionary of registered processors keyed by processor name.
    /// Value contains metadata about the processor.
    /// </summary>
    public Dictionary<string, ProcessorMetadata> Processors { get; } = new();
}