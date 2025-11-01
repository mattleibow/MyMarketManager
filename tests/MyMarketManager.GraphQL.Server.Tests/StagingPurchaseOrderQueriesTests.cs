using Microsoft.EntityFrameworkCore;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
using MyMarketManager.GraphQL.Server;
using MyMarketManager.Tests.Shared;

namespace MyMarketManager.GraphQL.Server.Tests;

[Trait(TestCategories.Key, TestCategories.Values.GraphQL)]
public class StagingPurchaseOrderQueriesTests(ITestOutputHelper outputHelper) : SqliteTestBase(outputHelper, createSchema: true)
{
    private StagingPurchaseOrderQueries Queries => new();

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
        var result = await Queries.GetStagingPurchaseOrderById(order.Id, Context, Cancel);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(order.Id, result.Id);
        Assert.Equal("PO-001", result.SupplierReference);
        Assert.Equal(supplier.Name, result.SupplierName);
        Assert.Single(result.Items);
        Assert.Equal("Test Item", result.Items[0].Name);
        Assert.Equal("Test Description", result.Items[0].Description);
        Assert.Equal(5, result.Items[0].Quantity);
        Assert.NotNull(result.Items[0].Product);
        Assert.Equal(product.Name, result.Items[0].Product!.Name);
    }

    [Fact]
    public async Task GetStagingPurchaseOrderById_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await Queries.GetStagingPurchaseOrderById(nonExistentId, Context, Cancel);

        // Assert
        Assert.Null(result);
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
        var result = await Queries.SearchProductsForItem("Widget", Context, Cancel);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Name == "Red Widget");
        Assert.Contains(result, p => p.Name == "Blue Widget");
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
        var result = await Queries.SearchProductsForItem("widget", Context, Cancel);

        // Assert
        Assert.Single(result);
        Assert.Equal("Product A", result[0].Name);
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
        var result = await Queries.SearchProductsForItem("WID", Context, Cancel);

        // Assert
        Assert.Single(result);
        Assert.Equal("Product A", result[0].Name);
        Assert.Equal("WID-001", result[0].SKU);
    }

    [Fact]
    public async Task SearchProductsForItem_ShouldReturnMaximum50Results()
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
        var result = await Queries.SearchProductsForItem("Widget", Context, Cancel);

        // Assert
        Assert.Equal(50, result.Count);
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
        var result = await Queries.SearchProductsForItem("Widget", Context, Cancel);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Apple Widget", result[0].Name);
        Assert.Equal("Zebra Widget", result[1].Name);
    }
}
