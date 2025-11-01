using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
using MyMarketManager.Tests.Shared;

namespace MyMarketManager.GraphQL.Server.Tests;

[Trait(TestCategories.Key, TestCategories.Values.GraphQL)]
public class StagingPurchaseOrderQueriesTests(ITestOutputHelper outputHelper) : GraphQLTestBase(outputHelper, createSchema: true)
{
    [Fact]
    public async Task GetStagingPurchaseOrderById_WithValidId_ShouldReturnDetailWithItems()
    {
        // Arrange
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Test Supplier" };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Quality = ProductQuality.Good,
            StockOnHand = 0
        };
        Context.Suppliers.Add(supplier);
        Context.Products.Add(product);

        var batch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            BatchType = StagingBatchType.WebScrape,
            SupplierId = supplier.Id,
            StartedAt = DateTimeOffset.UtcNow,
            Status = ProcessingStatus.Completed
        };
        Context.StagingBatches.Add(batch);

        var order = new StagingPurchaseOrder
        {
            Id = Guid.NewGuid(),
            StagingBatchId = batch.Id,
            SupplierReference = "PO-001",
            OrderDate = DateTimeOffset.UtcNow,
            Status = ProcessingStatus.Completed,
            IsImported = false
        };
        Context.StagingPurchaseOrders.Add(order);

        var item = new StagingPurchaseOrderItem
        {
            Id = Guid.NewGuid(),
            StagingPurchaseOrderId = order.Id,
            ProductId = product.Id,
            Name = "Test Item",
            Description = "Test Description",
            SupplierReference = "SKU-001",
            Quantity = 5,
            ListedUnitPrice = 10m,
            ActualUnitPrice = 9m,
            Status = CandidateStatus.Linked,
            IsImported = false
        };
        Context.StagingPurchaseOrderItems.Add(item);
        await Context.SaveChangesAsync(Cancel);

        // Act
        var result = await ExecuteQueryAsync<StagingPurchaseOrderByIdResponse>($$"""
            query {
                stagingPurchaseOrderById(id: "{{order.Id}}") {
                    id
                    supplierReference
                    supplierName
                    items {
                        name
                        description
                        quantity
                        product {
                            name
                        }
                    }
                }
            }
        """);

        // Assert
        Assert.NotNull(result.StagingPurchaseOrderById);
        Assert.Equal(order.Id, result.StagingPurchaseOrderById.Id);
        Assert.Equal("PO-001", result.StagingPurchaseOrderById.SupplierReference);
        Assert.Equal(supplier.Name, result.StagingPurchaseOrderById.SupplierName);
        Assert.Single(result.StagingPurchaseOrderById.Items);
        Assert.Equal("Test Item", result.StagingPurchaseOrderById.Items[0].Name);
        Assert.Equal("Test Description", result.StagingPurchaseOrderById.Items[0].Description);
        Assert.Equal(5, result.StagingPurchaseOrderById.Items[0].Quantity);
        Assert.NotNull(result.StagingPurchaseOrderById.Items[0].Product);
        Assert.Equal(product.Name, result.StagingPurchaseOrderById.Items[0].Product!.Name);
    }

    [Fact]
    public async Task GetStagingPurchaseOrderById_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await ExecuteQueryAsync<StagingPurchaseOrderByIdResponse>($$"""
            query {
                stagingPurchaseOrderById(id: "{{nonExistentId}}") {
                    id
                }
            }
        """);

        // Assert
        Assert.Null(result.StagingPurchaseOrderById);
    }

    [Fact]
    public async Task SearchProductsForItem_ShouldFindProductsByName()
    {
        // Arrange
        var product1 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Red Widget",
            Quality = ProductQuality.Good,
            StockOnHand = 10
        };
        var product2 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Blue Widget",
            Quality = ProductQuality.Fair,
            StockOnHand = 5
        };
        var product3 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Green Gadget",
            Quality = ProductQuality.Excellent,
            StockOnHand = 15
        };

        Context.Products.AddRange(product1, product2, product3);
        await Context.SaveChangesAsync(Cancel);

        // Act
        var result = await ExecuteQueryAsync<SearchProductsResponse>("""
            query {
                searchProductsForItem(where: { name: { contains: "Widget" } }) {
                    id
                    name
                    quality
                    stockOnHand
                }
            }
        """);

        // Assert
        Assert.Equal(2, result.SearchProductsForItem.Count);
        Assert.Contains(result.SearchProductsForItem, p => p.Name == "Red Widget");
        Assert.Contains(result.SearchProductsForItem, p => p.Name == "Blue Widget");
    }

    [Fact]
    public async Task SearchProductsForItem_ShouldFindProductsByDescription()
    {
        // Arrange
        var product1 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Product A",
            Description = "This is a widget",
            Quality = ProductQuality.Good,
            StockOnHand = 10
        };
        var product2 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Product B",
            Description = "This is a gadget",
            Quality = ProductQuality.Good,
            StockOnHand = 5
        };

        Context.Products.AddRange(product1, product2);
        await Context.SaveChangesAsync(Cancel);

        // Act
        var result = await ExecuteQueryAsync<SearchProductsResponse>("""
            query {
                searchProductsForItem(where: { description: { contains: "widget" } }) {
                    id
                    name
                }
            }
        """);

        // Assert
        Assert.Single(result.SearchProductsForItem);
        Assert.Equal("Product A", result.SearchProductsForItem[0].Name);
    }

    [Fact]
    public async Task SearchProductsForItem_ShouldFindProductsBySKU()
    {
        // Arrange
        var product1 = new Product
        {
            Id = Guid.NewGuid(),
            SKU = "WID-001",
            Name = "Product A",
            Quality = ProductQuality.Good,
            StockOnHand = 10
        };
        var product2 = new Product
        {
            Id = Guid.NewGuid(),
            SKU = "GAD-001",
            Name = "Product B",
            Quality = ProductQuality.Good,
            StockOnHand = 5
        };

        Context.Products.AddRange(product1, product2);
        await Context.SaveChangesAsync(Cancel);

        // Act
        var result = await ExecuteQueryAsync<SearchProductsResponse>("""
            query {
                searchProductsForItem(where: { sku: { contains: "WID" } }) {
                    id
                    name
                    sku
                }
            }
        """);

        // Assert
        Assert.Single(result.SearchProductsForItem);
        Assert.Equal("Product A", result.SearchProductsForItem[0].Name);
        Assert.Equal("WID-001", result.SearchProductsForItem[0].SKU);
    }

    [Fact]
    public async Task SearchProductsForItem_ShouldFilterAndReturnResults()
    {
        // Arrange
        for (int i = 0; i < 100; i++)
        {
            Context.Products.Add(new Product
            {
                Id = Guid.NewGuid(),
                Name = $"Widget {i}",
                Quality = ProductQuality.Good,
                StockOnHand = i
            });
        }
        await Context.SaveChangesAsync(Cancel);

        // Act
        var result = await ExecuteQueryAsync<SearchProductsResponse>("""
            query {
                searchProductsForItem(where: { name: { contains: "Widget" } }) {
                    id
                    name
                }
            }
        """);

        // Assert - All 100 widgets should be returned (no manual limit anymore)
        Assert.Equal(100, result.SearchProductsForItem.Count);
    }

    [Fact]
    public async Task SearchProductsForItem_ShouldOrderByName()
    {
        // Arrange
        var product1 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Zebra Widget",
            Quality = ProductQuality.Good,
            StockOnHand = 10
        };
        var product2 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Apple Widget",
            Quality = ProductQuality.Good,
            StockOnHand = 5
        };

        Context.Products.AddRange(product1, product2);
        await Context.SaveChangesAsync(Cancel);

        // Act
        var result = await ExecuteQueryAsync<SearchProductsResponse>("""
            query {
                searchProductsForItem(
                    where: { name: { contains: "Widget" } }
                    order: { name: ASC }
                ) {
                    id
                    name
                }
            }
        """);

        // Assert
        Assert.Equal(2, result.SearchProductsForItem.Count);
        Assert.Equal("Apple Widget", result.SearchProductsForItem[0].Name);
        Assert.Equal("Zebra Widget", result.SearchProductsForItem[1].Name);
    }

    private record StagingPurchaseOrderByIdResponse(StagingPurchaseOrderDetailDto? StagingPurchaseOrderById);
    private record StagingPurchaseOrderDetailDto(Guid Id, string? SupplierReference, string? SupplierName, List<StagingPurchaseOrderItemDto> Items);
    private record StagingPurchaseOrderItemDto(string Name, string? Description, int Quantity, ProductDto? Product);
    private record ProductDto(string Name);
    private record SearchProductsResponse(List<ProductSearchDto> SearchProductsForItem);
    private record ProductSearchDto(Guid Id, string Name, string? SKU, string? Description, ProductQuality Quality, int StockOnHand);
}

