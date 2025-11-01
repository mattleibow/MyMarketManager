using HotChocolate;
using Microsoft.EntityFrameworkCore;
using MyMarketManager.Data;
using MyMarketManager.Data.Entities;

namespace MyMarketManager.GraphQL.Server;

/// <summary>
/// GraphQL queries for purchase orders
/// </summary>
[ExtendObjectType("Query")]
public class PurchaseOrderQueries
{
    /// <summary>
    /// Get all purchase orders with filtering, sorting, and projection support
    /// Default ordering: OrderDate descending (most recent first)
    /// </summary>
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<PurchaseOrder> GetPurchaseOrders(
        MyMarketManagerDbContext context)
    {
        return context.PurchaseOrders.OrderByDescending(po => po.OrderDate);
    }

    /// <summary>
    /// Get a purchase order by ID
    /// </summary>
    [UseProjection]
    [UseSingleOrDefault]
    public IQueryable<PurchaseOrder> GetPurchaseOrderById(
        Guid id,
        MyMarketManagerDbContext context)
    {
        return context.PurchaseOrders.Where(po => po.Id == id);
    }
}
