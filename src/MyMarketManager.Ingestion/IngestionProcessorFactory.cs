using Microsoft.Extensions.DependencyInjection;

namespace MyMarketManager.Ingestion;

/// <summary>
/// Factory for creating ingestion processor instances from DI.
/// </summary>
public class IngestionProcessorFactory : IIngestionProcessorFactory
{
    private readonly IServiceProvider _serviceProvider;

    public IngestionProcessorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Gets all registered ingestion processors from DI.
    /// </summary>
    public IEnumerable<IIngestionProcessor> GetProcessors()
    {
        return _serviceProvider.GetServices<IIngestionProcessor>();
    }
}
