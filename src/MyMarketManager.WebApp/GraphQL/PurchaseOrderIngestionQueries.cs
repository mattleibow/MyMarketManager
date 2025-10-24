using HotChocolate;
using Microsoft.EntityFrameworkCore;
using MyMarketManager.Data;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
using MyMarketManager.Scrapers;

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
        [Service] BatchProcessorFactory factory)
    {
        return factory.GetAvailableProcessors(batchType);
    }

}
