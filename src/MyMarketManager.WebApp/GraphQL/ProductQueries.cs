using Microsoft.EntityFrameworkCore;
using MyMarketManager.Data;
using MyMarketManager.Data.Entities;

namespace MyMarketManager.WebApp.GraphQL;

/// <summary>
/// GraphQL queries for products
/// </summary>
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

    /// <summary>
    /// Search products by name, description, or SKU
    /// </summary>
    public async Task<List<Product>> SearchProducts(
        string searchTerm,
        MyMarketManagerDbContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await context.Products
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);
        }

        return await context.Products
            .Where(p => p.Name.Contains(searchTerm) ||
                       (p.Description != null && p.Description.Contains(searchTerm)) ||
                       (p.SKU != null && p.SKU.Contains(searchTerm)))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }
}
