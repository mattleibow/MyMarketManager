using MyMarketManager.Data.Processing;

namespace MyMarketManager.WebApp.GraphQL;

/// <summary>
/// GraphQL queries for PO ingestion
/// </summary>
[ExtendObjectType("Query")]
public class PurchaseOrderIngestionQueries
{
    /// <summary>
    /// Get available scrapers (ingestion processors)
    /// </summary>
    public IEnumerable<string> GetAvailableScrapers(
        [Service] WorkItemProcessingEngine engine)
    {
        return engine.GetHandlerNamesByPurpose(ProcessorPurpose.Ingestion);
    }
}
