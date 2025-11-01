using Microsoft.EntityFrameworkCore;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
using MyMarketManager.GraphQL.Server;
using MyMarketManager.Tests.Shared;

namespace MyMarketManager.GraphQL.Server.Tests;

[Trait(TestCategories.Key, TestCategories.Values.GraphQL)]
public class StagingPurchaseOrderMutationsTests(ITestOutputHelper outputHelper) : SqliteTestBase(outputHelper, createSchema: true)
{
    private StagingPurchaseOrderMutations Mutations => new();

    [Fact]
    public async Task LinkStagingItemToProduct_WithValidIds_ShouldLinkItem()
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.NewGuid(),
            SKU = "TEST-001",
            Name = "Test Product",
            Quality = ProductQuality.Good,
            StockOnHand = 10
        };
        Context.Products.Add(product);

        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Test Supplier" };
        Context.Suppliers.Add(supplier);

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
            OrderDate = DateTimeOffset.UtcNow,
            Status = ProcessingStatus.Completed
        };
        Context.StagingPurchaseOrders.Add(order);

        var item = new StagingPurchaseOrderItem
        {
            Id = Guid.NewGuid(),
            StagingPurchaseOrderId = order.Id,
            Name = "Item to Link",
            Quantity = 1,
            ListedUnitPrice = 10m,
            ActualUnitPrice = 10m,
            Status = CandidateStatus.Pending
        };
        Context.StagingPurchaseOrderItems.Add(item);
        await Context.SaveChangesAsync(Cancel);

        var input = new LinkStagingItemToProductInput(
            StagingItemId: item.Id,
            ProductId: product.Id
        );

        // Act
        var result = await Mutations.LinkStagingItemToProduct(input, Context, Cancel);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.LinkedProduct);
        Assert.Equal(product.Id, result.LinkedProduct.Id);
        Assert.Equal(product.Name, result.LinkedProduct.Name);

        // Verify database was updated
        var dbItem = await Context.StagingPurchaseOrderItems.FindAsync(new object[] { item.Id }, Cancel);
        Assert.NotNull(dbItem);
        Assert.Equal(product.Id, dbItem.ProductId);
        Assert.Equal(CandidateStatus.Linked, dbItem.Status);
    }

    [Fact]
    public async Task LinkStagingItemToProduct_WithInvalidItemId_ShouldReturnError()
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Quality = ProductQuality.Good,
            StockOnHand = 10
        };
        Context.Products.Add(product);
        await Context.SaveChangesAsync(Cancel);

        var input = new LinkStagingItemToProductInput(
            StagingItemId: Guid.NewGuid(),
            ProductId: product.Id
        );

        // Act
        var result = await Mutations.LinkStagingItemToProduct(input, Context, Cancel);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Staging item not found", result.ErrorMessage);
        Assert.Null(result.LinkedProduct);
    }

    [Fact]
    public async Task LinkStagingItemToProduct_WithInvalidProductId_ShouldReturnError()
    {
        // Arrange
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Test Supplier" };
        Context.Suppliers.Add(supplier);

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
            OrderDate = DateTimeOffset.UtcNow,
            Status = ProcessingStatus.Completed
        };
        Context.StagingPurchaseOrders.Add(order);

        var item = new StagingPurchaseOrderItem
        {
            Id = Guid.NewGuid(),
            StagingPurchaseOrderId = order.Id,
            Name = "Item to Link",
            Quantity = 1,
            ListedUnitPrice = 10m,
            ActualUnitPrice = 10m,
            Status = CandidateStatus.Pending
        };
        Context.StagingPurchaseOrderItems.Add(item);
        await Context.SaveChangesAsync(Cancel);

        var input = new LinkStagingItemToProductInput(
            StagingItemId: item.Id,
            ProductId: Guid.NewGuid()
        );

        // Act
        var result = await Mutations.LinkStagingItemToProduct(input, Context, Cancel);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Product not found", result.ErrorMessage);
        Assert.Null(result.LinkedProduct);
    }

    [Fact]
    public async Task UnlinkStagingItemFromProduct_WithValidId_ShouldUnlinkItem()
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Quality = ProductQuality.Good,
            StockOnHand = 10
        };
        Context.Products.Add(product);

        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Test Supplier" };
        Context.Suppliers.Add(supplier);

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
            OrderDate = DateTimeOffset.UtcNow,
            Status = ProcessingStatus.Completed
        };
        Context.StagingPurchaseOrders.Add(order);

        var item = new StagingPurchaseOrderItem
        {
            Id = Guid.NewGuid(),
            StagingPurchaseOrderId = order.Id,
            ProductId = product.Id,
            Name = "Linked Item",
            Quantity = 1,
            ListedUnitPrice = 10m,
            ActualUnitPrice = 10m,
            Status = CandidateStatus.Linked
        };
        Context.StagingPurchaseOrderItems.Add(item);
        await Context.SaveChangesAsync(Cancel);

        // Act
        var result = await Mutations.UnlinkStagingItemFromProduct(item.Id, Context, Cancel);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);

        // Verify database was updated
        var dbItem = await Context.StagingPurchaseOrderItems.FindAsync(new object[] { item.Id }, Cancel);
        Assert.NotNull(dbItem);
        Assert.Null(dbItem.ProductId);
        Assert.Equal(CandidateStatus.Pending, dbItem.Status);
    }

    [Fact]
    public async Task UnlinkStagingItemFromProduct_WithInvalidId_ShouldReturnError()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await Mutations.UnlinkStagingItemFromProduct(nonExistentId, Context, Cancel);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Staging item not found", result.ErrorMessage);
    }
}
