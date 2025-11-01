using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Processing;

/// <summary>
/// Factory that creates batch processors based on batch type and processor name.
/// Centralizes processor registration in one place.
/// </summary>
internal class BatchProcessorFactory(IServiceProvider serviceProvider, IOptions<BatchProcessorOptions> options) : IBatchProcessorFactory
{
    /// <summary>
    /// Gets a processor for the given processor name.
    /// </summary>
    public IBatchProcessor? GetProcessor(string processorName)
    {
        if (string.IsNullOrWhiteSpace(processorName))
        {
            return null;
        }

        var processors = options.Value.Processors;
        if (!processors.TryGetValue(processorName, out var processorInfo))
        {
            return null;
        }

        return serviceProvider.GetRequiredService(processorInfo.ProcessorType) as IBatchProcessor;
    }

    /// <summary>
    /// Gets all available processor names for a given batch type.
    /// </summary>
    public IEnumerable<string> GetAvailableProcessors(StagingBatchType batchType)
    {
        return options.Value.Processors
            .Where(kvp => kvp.Value.BatchType == batchType)
            .Select(kvp => kvp.Key);
    }
}
