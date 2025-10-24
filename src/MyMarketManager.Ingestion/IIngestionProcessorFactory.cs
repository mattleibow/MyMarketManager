namespace MyMarketManager.Ingestion;

/// <summary>
/// Factory for creating ingestion processor instances.
/// </summary>
public interface IIngestionProcessorFactory
{
    /// <summary>
    /// Gets all registered ingestion processors.
    /// </summary>
    /// <returns>Collection of all available processors.</returns>
    IEnumerable<IIngestionProcessor> GetProcessors();
}
