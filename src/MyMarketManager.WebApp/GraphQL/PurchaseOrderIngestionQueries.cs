using MyMarketManager.Processing;

namespace MyMarketManager.WebApp.GraphQL;

/// <summary>
/// GraphQL queries for PO ingestion
/// </summary>
[ExtendObjectType("Query")]
public class PurchaseOrderIngestionQueries
{
    /// <summary>
    /// Get available ingestion processors
    /// </summary>
    public IEnumerable<string> GetAvailableIngestionProcessors(
        [Service] WorkItemProcessingService processingService)
    {
        return processingService.GetHandlers(WorkItemHandlerPurpose.Ingestion);
    }
}
