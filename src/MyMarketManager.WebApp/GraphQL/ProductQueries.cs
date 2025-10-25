using Microsoft.EntityFrameworkCore;
using MyMarketManager.Data;
using MyMarketManager.Data.Entities;

namespace MyMarketManager.WebApp.GraphQL;

/// <summary>
/// GraphQL queries for products
/// </summary>
[ExtendObjectType("Query")]
public class ProductQueries
{
    /// <summary>
    /// Get all products with filtering and sorting support
    /// </summary>
    public IQueryable<Product> GetProducts(MyMarketManagerDbContext context)
    {
        return context.Products.OrderBy(p => p.Name);
    }

    /// <summary>
    /// Get a product by ID
    /// </summary>
    public async Task<Product?> GetProductById(
        Guid id,
        MyMarketManagerDbContext context,
        CancellationToken cancellationToken)
    {
        return await context.Products.FindAsync(new object[] { id }, cancellationToken);
    }
}
