using HotChocolate;
using Microsoft.EntityFrameworkCore;
using MyMarketManager.Data;
using MyMarketManager.Data.Entities;

namespace MyMarketManager.GraphQL.Server;

/// <summary>
/// GraphQL queries for products
/// </summary>
[ExtendObjectType("Query")]
public class ProductQueries
{
    /// <summary>
    /// Get all products with filtering, sorting, and projection support
    /// </summary>
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Product> GetProducts(MyMarketManagerDbContext context)
    {
        return context.Products;
    }

    /// <summary>
    /// Get a product by ID
    /// </summary>
    [UseProjection]
    [UseSingleOrDefault]
    public IQueryable<Product> GetProductById(
        Guid id,
        MyMarketManagerDbContext context)
    {
        return context.Products.Where(p => p.Id == id);
    }
}
