using HotChocolate;
using Microsoft.EntityFrameworkCore;
using MyMarketManager.Data;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.GraphQL.Server;

/// <summary>
/// GraphQL mutations for products
/// </summary>
[ExtendObjectType("Mutation")]
public class ProductMutations
{
    /// <summary>
    /// Create a new product
    /// </summary>
    public async Task<Product> CreateProduct(
        CreateProductInput input,
        MyMarketManagerDbContext context,
        CancellationToken cancellationToken)
    {
        var product = new Product
        {
            SKU = input.SKU,
            Name = input.Name,
            Description = input.Description,
            Quality = input.Quality,
            Notes = input.Notes,
            StockOnHand = input.StockOnHand
        };

        context.Products.Add(product);
        await context.SaveChangesAsync(cancellationToken);

        return product;
    }

    /// <summary>
    /// Update an existing product
    /// </summary>
    public async Task<Product> UpdateProduct(
        Guid id,
        UpdateProductInput input,
        MyMarketManagerDbContext context,
        CancellationToken cancellationToken)
    {
        var product = await context.Products.FindAsync(new object[] { id }, cancellationToken);
        if (product == null)
        {
            throw new GraphQLException("Product not found");
        }

        product.SKU = input.SKU;
        product.Name = input.Name;
        product.Description = input.Description;
        product.Quality = input.Quality;
        product.Notes = input.Notes;
        product.StockOnHand = input.StockOnHand;

        await context.SaveChangesAsync(cancellationToken);

        return product;
    }

    /// <summary>
    /// Delete a product
    /// </summary>
    public async Task<bool> DeleteProduct(
        Guid id,
        MyMarketManagerDbContext context,
        CancellationToken cancellationToken)
    {
        var product = await context.Products.FindAsync(new object[] { id }, cancellationToken);
        if (product == null)
        {
            throw new GraphQLException("Product not found");
        }

        context.Products.Remove(product);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}

/// <summary>
/// Input type for creating a product
/// </summary>
public record CreateProductInput(
    string? SKU,
    string Name,
    string? Description,
    ProductQuality Quality,
    string? Notes,
    int StockOnHand
);

/// <summary>
/// Input type for updating a product
/// </summary>
public record UpdateProductInput(
    string? SKU,
    string Name,
    string? Description,
    ProductQuality Quality,
    string? Notes,
    int StockOnHand
);
