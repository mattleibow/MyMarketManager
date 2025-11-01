using Microsoft.EntityFrameworkCore;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
using MyMarketManager.Tests.Shared;

namespace MyMarketManager.GraphQL.Server.Tests;

[Trait(TestCategories.Key, TestCategories.Values.GraphQL)]
public class ProductQueriesTests(ITestOutputHelper outputHelper) : GraphQLTestBase(outputHelper, createSchema: true)
{
    [Fact]
    public async Task GetProducts_ShouldReturnAllProducts()
    {
        // Arrange
        var product1 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Zebra Product",
            Quality = ProductQuality.Good,
            StockOnHand = 10
        };
        var product2 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Apple Product",
            Quality = ProductQuality.Good,
            StockOnHand = 20
        };

        Context.Products.AddRange(product1, product2);
        await Context.SaveChangesAsync(Cancel);

        // Act
        var result = await ExecuteQueryAsync<ProductsResponse>(@"
            query {
                products {
                    id
                    name
                    quality
                    stockOnHand
                }
            }
        ");

        // Assert
        Assert.Equal(2, result.Products.Count);
        Assert.Contains(result.Products, p => p.Name == "Apple Product");
        Assert.Contains(result.Products, p => p.Name == "Zebra Product");
    }

    [Fact]
    public async Task GetProducts_WithSorting_ShouldReturnSortedProducts()
    {
        // Arrange
        var product1 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Zebra Product",
            Quality = ProductQuality.Good,
            StockOnHand = 10
        };
        var product2 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Apple Product",
            Quality = ProductQuality.Good,
            StockOnHand = 20
        };

        Context.Products.AddRange(product1, product2);
        await Context.SaveChangesAsync(Cancel);

        // Act
        var result = await ExecuteQueryAsync<ProductsResponse>(@"
            query {
                products(order: { name: ASC }) {
                    id
                    name
                }
            }
        ");

        // Assert
        Assert.Equal(2, result.Products.Count);
        Assert.Equal("Apple Product", result.Products[0].Name);
        Assert.Equal("Zebra Product", result.Products[1].Name);
    }

    [Fact]
    public async Task GetProducts_ShouldNotIncludeDeletedProducts()
    {
        // Arrange
        var product1 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Active Product",
            Quality = ProductQuality.Good,
            StockOnHand = 10
        };
        var product2 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Deleted Product",
            Quality = ProductQuality.Good,
            StockOnHand = 20,
            DeletedAt = DateTimeOffset.UtcNow
        };

        Context.Products.AddRange(product1, product2);
        await Context.SaveChangesAsync(Cancel);

        // Act
        var result = await ExecuteQueryAsync<ProductsResponse>(@"
            query {
                products {
                    id
                    name
                }
            }
        ");

        // Assert
        Assert.Single(result.Products);
        Assert.Equal("Active Product", result.Products[0].Name);
    }

    [Fact]
    public async Task GetProductById_WithValidId_ShouldReturnProduct()
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Quality = ProductQuality.Good,
            StockOnHand = 15
        };

        Context.Products.Add(product);
        await Context.SaveChangesAsync(Cancel);

        // Act
        var result = await ExecuteQueryAsync<ProductByIdResponse>(@"
            query($id: UUID!) {
                productById(id: $id) {
                    id
                    name
                    quality
                    stockOnHand
                }
            }
        ", new Dictionary<string, object?> { ["id"] = product.Id });

        // Assert
        Assert.NotNull(result.ProductById);
        Assert.Equal(product.Id, result.ProductById.Id);
        Assert.Equal("Test Product", result.ProductById.Name);
    }

    [Fact]
    public async Task GetProductById_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await ExecuteQueryAsync<ProductByIdResponse>(@"
            query($id: UUID!) {
                productById(id: $id) {
                    id
                    name
                }
            }
        ", new Dictionary<string, object?> { ["id"] = nonExistentId });

        // Assert
        Assert.Null(result.ProductById);
    }

    [Fact]
    public async Task GetProductById_WithDeletedProduct_ShouldReturnNull()
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Deleted Product",
            Quality = ProductQuality.Good,
            StockOnHand = 10,
            DeletedAt = DateTimeOffset.UtcNow
        };

        Context.Products.Add(product);
        await Context.SaveChangesAsync(Cancel);

        // Act
        var result = await ExecuteQueryAsync<ProductByIdResponse>(@"
            query($id: UUID!) {
                productById(id: $id) {
                    id
                    name
                }
            }
        ", new Dictionary<string, object?> { ["id"] = product.Id });

        // Assert
        // Query filter will exclude deleted products
        Assert.Null(result.ProductById);
    }

    private record ProductsResponse(List<ProductDto> Products);
    private record ProductByIdResponse(ProductDto? ProductById);
    private record ProductDto(Guid Id, string Name, ProductQuality Quality, int StockOnHand);
}

