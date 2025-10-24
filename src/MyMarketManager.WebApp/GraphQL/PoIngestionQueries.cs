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
public class PoIngestionQueries
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

    /// <summary>
    /// Check if a cookie hash already exists
    /// </summary>
    public async Task<bool> CheckCookieDuplicate(
        string cookieHash,
        MyMarketManagerDbContext context,
        CancellationToken cancellationToken)
    {
        return await context.StagingBatches
            .AnyAsync(b => b.FileHash == cookieHash, cancellationToken);
    }

    /// <summary>
    /// Get all suppliers for dropdown
    /// </summary>
    public async Task<List<SupplierOption>> GetSuppliers(
        MyMarketManagerDbContext context,
        CancellationToken cancellationToken)
    {
        return await context.Suppliers
            .OrderBy(s => s.Name)
            .Select(s => new SupplierOption(s.Id, s.Name))
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// Supplier option for dropdown
/// </summary>
public record SupplierOption(Guid Id, string Name);
