using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
using MyMarketManager.Tests.Shared;

namespace MyMarketManager.GraphQL.Server.Tests;

[Trait(TestCategories.Key, TestCategories.Values.GraphQL)]
public class StagingPurchaseOrderMutationsTests(ITestOutputHelper outputHelper) : GraphQLTestBase(outputHelper, createSchema: true)
{
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

        // Act
        var result = await ExecuteQueryAsync<LinkStagingItemResponse>($$"""
            mutation {
                linkStagingItemToProduct(input: {
                    stagingItemId: "{{item.Id}}"
                    productId: "{{product.Id}}"
                }) {
                    success
                    errorMessage
                    linkedProduct {
                        id
                        name
                    }
                }
            }
        """);

        // Assert
        Assert.True(result.LinkStagingItemToProduct.Success);
        Assert.Null(result.LinkStagingItemToProduct.ErrorMessage);
        Assert.NotNull(result.LinkStagingItemToProduct.LinkedProduct);
        Assert.Equal(product.Id, result.LinkStagingItemToProduct.LinkedProduct.Id);
        Assert.Equal(product.Name, result.LinkStagingItemToProduct.LinkedProduct.Name);

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

        var invalidItemId = Guid.NewGuid();

        // Act
        var result = await ExecuteQueryAsync<LinkStagingItemResponse>($$"""
            mutation {
                linkStagingItemToProduct(input: {
                    stagingItemId: "{{invalidItemId}}"
                    productId: "{{product.Id}}"
                }) {
                    success
                    errorMessage
                    linkedProduct {
                        id
                    }
                }
            }
        """);

        // Assert
        Assert.False(result.LinkStagingItemToProduct.Success);
        Assert.Equal("Staging item not found", result.LinkStagingItemToProduct.ErrorMessage);
        Assert.Null(result.LinkStagingItemToProduct.LinkedProduct);
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

        var invalidProductId = Guid.NewGuid();

        // Act
        var result = await ExecuteQueryAsync<LinkStagingItemResponse>($$"""
            mutation {
                linkStagingItemToProduct(input: {
                    stagingItemId: "{{item.Id}}"
                    productId: "{{invalidProductId}}"
                }) {
                    success
                    errorMessage
                    linkedProduct {
                        id
                    }
                }
            }
        """);

        // Assert
        Assert.False(result.LinkStagingItemToProduct.Success);
        Assert.Equal("Product not found", result.LinkStagingItemToProduct.ErrorMessage);
        Assert.Null(result.LinkStagingItemToProduct.LinkedProduct);
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
        var result = await ExecuteQueryAsync<UnlinkStagingItemResponse>($$"""
            mutation {
                unlinkStagingItemFromProduct(stagingItemId: "{{item.Id}}") {
                    success
                    errorMessage
                }
            }
        """);

        // Assert
        Assert.True(result.UnlinkStagingItemFromProduct.Success);
        Assert.Null(result.UnlinkStagingItemFromProduct.ErrorMessage);

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
        var result = await ExecuteQueryAsync<UnlinkStagingItemResponse>($$"""
            mutation {
                unlinkStagingItemFromProduct(stagingItemId: "{{nonExistentId}}") {
                    success
                    errorMessage
                }
            }
        """);

        // Assert
        Assert.False(result.UnlinkStagingItemFromProduct.Success);
        Assert.Equal("Staging item not found", result.UnlinkStagingItemFromProduct.ErrorMessage);
    }

    private record LinkStagingItemResponse(LinkStagingItemResultDto LinkStagingItemToProduct);
    private record LinkStagingItemResultDto(bool Success, string? ErrorMessage, LinkedProductDto? LinkedProduct);
    private record LinkedProductDto(Guid Id, string Name);
    private record UnlinkStagingItemResponse(UnlinkStagingItemResultDto UnlinkStagingItemFromProduct);
    private record UnlinkStagingItemResultDto(bool Success, string? ErrorMessage);
}

