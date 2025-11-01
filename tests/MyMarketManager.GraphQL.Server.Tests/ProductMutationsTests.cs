using HotChocolate;
using Microsoft.EntityFrameworkCore;
using MyMarketManager.Data;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
using MyMarketManager.GraphQL.Server;
using MyMarketManager.Tests.Shared;

namespace MyMarketManager.GraphQL.Server.Tests;

[Trait(TestCategories.Key, TestCategories.Values.GraphQL)]
public class ProductMutationsTests(ITestOutputHelper outputHelper) : SqliteTestBase(outputHelper, createSchema: true)
{
    private ProductMutations Mutations => new();

    [Fact]
    public async Task CreateProduct_WithValidInput_ShouldCreateProduct()
    {
        // Arrange
        var input = new CreateProductInput(
            SKU: "TEST-001",
            Name: "Test Product",
            Description: "Test Description",
            Quality: ProductQuality.Good,
            Notes: "Test Notes",
            StockOnHand: 10
        );

        // Act
        var result = await Mutations.CreateProduct(input, Context, Cancel);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("TEST-001", result.SKU);
        Assert.Equal("Test Product", result.Name);
        Assert.Equal("Test Description", result.Description);
        Assert.Equal(ProductQuality.Good, result.Quality);
        Assert.Equal("Test Notes", result.Notes);
        Assert.Equal(10, result.StockOnHand);

        // Verify it's in the database
        var dbProduct = await Context.Products.FindAsync(result.Id);
        Assert.NotNull(dbProduct);
        Assert.Equal("Test Product", dbProduct.Name);
    }

    [Fact]
    public async Task CreateProduct_WithoutSKU_ShouldCreateProduct()
    {
        // Arrange
        var input = new CreateProductInput(
            SKU: null,
            Name: "No SKU Product",
            Description: null,
            Quality: ProductQuality.Fair,
            Notes: null,
            StockOnHand: 0
        );

        // Act
        var result = await Mutations.CreateProduct(input, Context, Cancel);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.SKU);
        Assert.Equal("No SKU Product", result.Name);
    }

    [Fact]
    public async Task UpdateProduct_WithValidId_ShouldUpdateProduct()
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.NewGuid(),
            SKU = "OLD-001",
            Name = "Old Name",
            Quality = ProductQuality.Good,
            StockOnHand = 5
        };
        Context.Products.Add(product);
        await Context.SaveChangesAsync(Cancel);

        var input = new UpdateProductInput(
            SKU: "NEW-001",
            Name: "New Name",
            Description: "New Description",
            Quality: ProductQuality.Excellent,
            Notes: "Updated",
            StockOnHand: 15
        );

        // Act
        var result = await Mutations.UpdateProduct(product.Id, input, Context, Cancel);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(product.Id, result.Id);
        Assert.Equal("NEW-001", result.SKU);
        Assert.Equal("New Name", result.Name);
        Assert.Equal("New Description", result.Description);
        Assert.Equal(ProductQuality.Excellent, result.Quality);
        Assert.Equal("Updated", result.Notes);
        Assert.Equal(15, result.StockOnHand);

        // Verify database was updated
        var dbProduct = await Context.Products.FindAsync(product.Id);
        Assert.NotNull(dbProduct);
        Assert.Equal("New Name", dbProduct.Name);
    }

    [Fact]
    public async Task UpdateProduct_WithInvalidId_ShouldThrowGraphQLException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var input = new UpdateProductInput(
            SKU: "TEST-001",
            Name: "Test",
            Description: null,
            Quality: ProductQuality.Good,
            Notes: null,
            StockOnHand: 10
        );

        // Act & Assert
        await Assert.ThrowsAsync<GraphQLException>(() =>
            Mutations.UpdateProduct(nonExistentId, input, Context, Cancel));
    }

    [Fact]
    public async Task DeleteProduct_WithValidId_ShouldDeleteProduct()
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "To Delete",
            Quality = ProductQuality.Good,
            StockOnHand = 5
        };
        Context.Products.Add(product);
        await Context.SaveChangesAsync(Cancel);

        // Act
        var result = await Mutations.DeleteProduct(product.Id, Context, Cancel);

        // Assert
        Assert.True(result);

        // Verify product is marked as deleted (soft delete)
        var dbProduct = await Context.Products
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == product.Id, Cancel);
        Assert.NotNull(dbProduct);
        Assert.True(dbProduct.IsDeleted);
    }

    [Fact]
    public async Task DeleteProduct_WithInvalidId_ShouldThrowGraphQLException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<GraphQLException>(() =>
            Mutations.DeleteProduct(nonExistentId, Context, Cancel));
    }
}
