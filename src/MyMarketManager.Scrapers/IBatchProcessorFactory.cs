using MyMarketManager.Data.Enums;
using MyMarketManager.Scrapers.Core;

namespace MyMarketManager.Scrapers;

/// <summary>
/// Factory for creating batch processors based on batch type and processor name.
/// </summary>
public interface IBatchProcessorFactory
{
    /// <summary>
    /// Gets a processor for the given batch type and processor name.
    /// </summary>
    /// <param name="batchType">The type of batch to process.</param>
    /// <param name="processorName">The name of the specific processor.</param>
    /// <returns>The processor, or null if not found.</returns>
    object? GetProcessor(StagingBatchType batchType, string processorName);

    /// <summary>
    /// Gets all available processor names for a given batch type.
    /// </summary>
    /// <param name="batchType">The type of batch.</param>
    /// <returns>Collection of available processor names.</returns>
    IEnumerable<string> GetAvailableProcessors(StagingBatchType batchType);
}
