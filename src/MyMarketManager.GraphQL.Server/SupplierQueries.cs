using HotChocolate;
using MyMarketManager.Data;
using MyMarketManager.Data.Entities;

namespace MyMarketManager.GraphQL.Server;

/// <summary>
/// GraphQL queries for suppliers
/// </summary>
[ExtendObjectType("Query")]
public class SupplierQueries
{
    /// <summary>
    /// Get all suppliers with filtering, sorting, and projection support
    /// Default ordering: Name ascending (alphabetical)
    /// </summary>
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Supplier> GetSuppliers(
        MyMarketManagerDbContext context)
    {
        return context.Suppliers.OrderBy(s => s.Name);
    }
}
