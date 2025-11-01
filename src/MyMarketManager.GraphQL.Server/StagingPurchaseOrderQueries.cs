using HotChocolate;
using Microsoft.EntityFrameworkCore;
using MyMarketManager.Data;
using MyMarketManager.Data.Entities;

namespace MyMarketManager.GraphQL.Server;

/// <summary>
/// GraphQL queries for staging purchase orders
/// </summary>
[ExtendObjectType("Query")]
public class StagingPurchaseOrderQueries
{
    /// <summary>
    /// Get a staging purchase order by ID with all items
    /// </summary>
    [UseProjection]
    [UseSingleOrDefault]
    public IQueryable<StagingPurchaseOrder> GetStagingPurchaseOrderById(
        Guid id,
        MyMarketManagerDbContext context)
    {
        return context.StagingPurchaseOrders.Where(spo => spo.Id == id);
    }

    /// <summary>
    /// Search products for matching suggestions based on name and description
    /// </summary>
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Product> SearchProductsForItem(
        MyMarketManagerDbContext context)
    {
        return context.Products;
    }
}
