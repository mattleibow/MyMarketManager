using Microsoft.Extensions.DependencyInjection;
using MyMarketManager.Data.Enums;
using MyMarketManager.Scrapers.Core;

namespace MyMarketManager.Scrapers;

/// <summary>
/// Factory that creates batch processors based on batch type and processor name.
/// Centralizes processor registration in one place.
/// </summary>
public class BatchProcessorFactory : IBatchProcessorFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<(StagingBatchType, string), Type> _processors = new();

    public BatchProcessorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Registers a processor type for a specific batch type and name.
    /// </summary>
    public void Register<TProcessor>(StagingBatchType batchType, string processorName)
        where TProcessor : class
    {
        _processors[(batchType, processorName)] = typeof(TProcessor);
    }

    /// <summary>
    /// Gets a processor for the given batch type and processor name.
    /// </summary>
    public object? GetProcessor(StagingBatchType batchType, string processorName)
    {
        var key = (batchType, processorName);
        if (!_processors.TryGetValue(key, out var processorType))
        {
            return null;
        }

        return _serviceProvider.GetRequiredService(processorType);
    }

    /// <summary>
    /// Gets all available processor names for a given batch type.
    /// </summary>
    public IEnumerable<string> GetAvailableProcessors(StagingBatchType batchType)
    {
        return _processors.Keys
            .Where(k => k.Item1 == batchType)
            .Select(k => k.Item2)
            .Distinct();
    }
}
