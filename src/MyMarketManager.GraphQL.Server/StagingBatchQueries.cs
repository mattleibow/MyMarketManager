using HotChocolate;
using MyMarketManager.Data;
using MyMarketManager.Data.Entities;

namespace MyMarketManager.GraphQL.Server;

/// <summary>
/// GraphQL queries for staging batches
/// </summary>
[ExtendObjectType("Query")]
public class StagingBatchQueries
{
    /// <summary>
    /// Get all staging batches with filtering, sorting, and projection support
    /// </summary>
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<StagingBatch> GetStagingBatches(
        MyMarketManagerDbContext context)
    {
        return context.StagingBatches;
    }

    /// <summary>
    /// Get a staging batch by ID
    /// </summary>
    [UseProjection]
    [UseSingleOrDefault]
    public IQueryable<StagingBatch> GetStagingBatchById(
        Guid id,
        MyMarketManagerDbContext context)
    {
        return context.StagingBatches.Where(sb => sb.Id == id);
    }
}
