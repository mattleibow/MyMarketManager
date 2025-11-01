using Microsoft.EntityFrameworkCore;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
using MyMarketManager.Tests.Shared;

namespace MyMarketManager.GraphQL.Server.Tests;

[Trait(TestCategories.Key, TestCategories.Values.GraphQL)]
public class ProductMutationsTests(ITestOutputHelper outputHelper) : GraphQLTestBase(outputHelper, createSchema: true)
{
    [Fact]
    public async Task CreateProduct_WithValidInput_ShouldCreateProduct()
    {
        // Arrange
        var input = new
        {
            SKU = "TEST-001",
            Name = "Test Product",
            Description = "Test Description",
            Quality = "GOOD",
            Notes = "Test Notes",
            StockOnHand = 10
        };

        // Act
        var result = await ExecuteQueryAsync<CreateProductResponse>($$"""
            mutation {
                createProduct(input: {
                    sku: "{{input.SKU}}"
                    name: "{{input.Name}}"
                    description: "{{input.Description}}"
                    quality: {{input.Quality}}
                    notes: "{{input.Notes}}"
                    stockOnHand: {{input.StockOnHand}}
                }) {
                    id
                    sku
                    name
                    description
                    quality
                    notes
                    stockOnHand
                }
            }
        """);

        // Assert
        Assert.NotNull(result.CreateProduct);
        Assert.NotEqual(Guid.Empty, result.CreateProduct.Id);
        Assert.Equal("TEST-001", result.CreateProduct.SKU);
        Assert.Equal("Test Product", result.CreateProduct.Name);
        Assert.Equal("Test Description", result.CreateProduct.Description);
        Assert.Equal(ProductQuality.Good, result.CreateProduct.Quality);
        Assert.Equal("Test Notes", result.CreateProduct.Notes);
        Assert.Equal(10, result.CreateProduct.StockOnHand);

        // Verify it's in the database
        var dbProduct = await Context.Products.FindAsync(result.CreateProduct.Id);
        Assert.NotNull(dbProduct);
        Assert.Equal("Test Product", dbProduct.Name);
    }

    [Fact]
    public async Task CreateProduct_WithoutSKU_ShouldCreateProduct()
    {
        // Arrange & Act
        var result = await ExecuteQueryAsync<CreateProductResponse>("""
            mutation {
                createProduct(input: {
                    name: "No SKU Product"
                    quality: FAIR
                    stockOnHand: 0
                }) {
                    id
                    sku
                    name
                }
            }
        """);

        // Assert
        Assert.NotNull(result.CreateProduct);
        Assert.Null(result.CreateProduct.SKU);
        Assert.Equal("No SKU Product", result.CreateProduct.Name);
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

        // Act
        var result = await ExecuteQueryAsync<UpdateProductResponse>($$"""
            mutation {
                updateProduct(
                    id: "{{product.Id}}"
                    input: {
                        sku: "NEW-001"
                        name: "New Name"
                        description: "New Description"
                        quality: EXCELLENT
                        notes: "Updated"
                        stockOnHand: 15
                    }
                ) {
                    id
                    sku
                    name
                    description
                    quality
                    notes
                    stockOnHand
                }
            }
        """);

        // Assert
        Assert.NotNull(result.UpdateProduct);
        Assert.Equal(product.Id, result.UpdateProduct.Id);
        Assert.Equal("NEW-001", result.UpdateProduct.SKU);
        Assert.Equal("New Name", result.UpdateProduct.Name);
        Assert.Equal("New Description", result.UpdateProduct.Description);
        Assert.Equal(ProductQuality.Excellent, result.UpdateProduct.Quality);
        Assert.Equal("Updated", result.UpdateProduct.Notes);
        Assert.Equal(15, result.UpdateProduct.StockOnHand);

        // Verify database was updated
        var dbProduct = await Context.Products.FindAsync(product.Id);
        Assert.NotNull(dbProduct);
        Assert.Equal("New Name", dbProduct.Name);
    }

    [Fact]
    public async Task UpdateProduct_WithInvalidId_ShouldReturnError()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await ExecuteRequestAsync($$"""
            mutation {
                updateProduct(
                    id: "{{nonExistentId}}"
                    input: {
                        sku: "TEST-001"
                        name: "Test"
                        quality: GOOD
                        stockOnHand: 10
                    }
                ) {
                    id
                }
            }
        """);

        // Assert
        var operationResult = result as HotChocolate.Execution.IOperationResult;
        Assert.NotNull(operationResult);
        Assert.NotNull(operationResult.Errors);
        Assert.NotEmpty(operationResult.Errors);
        Assert.Contains("Product not found", operationResult.Errors[0].Message);
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
        var result = await ExecuteQueryAsync<DeleteProductResponse>($$"""
            mutation {
                deleteProduct(id: "{{product.Id}}")
            }
        """);

        // Assert
        Assert.True(result.DeleteProduct);

        // Verify product is marked as deleted (soft delete)
        var dbProduct = await Context.Products
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == product.Id, Cancel);
        Assert.NotNull(dbProduct);
        Assert.True(dbProduct.IsDeleted);
    }

    [Fact]
    public async Task DeleteProduct_WithInvalidId_ShouldReturnError()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await ExecuteRequestAsync($$"""
            mutation {
                deleteProduct(id: "{{nonExistentId}}")
            }
        """);

        // Assert
        var operationResult = result as HotChocolate.Execution.IOperationResult;
        Assert.NotNull(operationResult);
        Assert.NotNull(operationResult.Errors);
        Assert.NotEmpty(operationResult.Errors);
        Assert.Contains("Product not found", operationResult.Errors[0].Message);
    }

    private record CreateProductResponse(ProductDto CreateProduct);
    private record UpdateProductResponse(ProductDto UpdateProduct);
    private record DeleteProductResponse(bool DeleteProduct);
    private record ProductDto(Guid Id, string? SKU, string Name, string? Description, ProductQuality Quality, string? Notes, int StockOnHand);
}

