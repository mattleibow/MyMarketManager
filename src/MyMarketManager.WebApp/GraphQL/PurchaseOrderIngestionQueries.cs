using MyMarketManager.Data.Enums;
using MyMarketManager.Data.Processing;

namespace MyMarketManager.WebApp.GraphQL;

/// <summary>
/// GraphQL queries for PO ingestion
/// </summary>
[ExtendObjectType("Query")]
public class PurchaseOrderIngestionQueries
{
    /// <summary>
    /// Get available scrapers for a batch type
    /// </summary>
    public IEnumerable<string> GetAvailableScrapers(
        StagingBatchType batchType,
        [Service] IBatchProcessorFactory factory)
    {
        return factory.GetAvailableProcessors(batchType);
    }
}
