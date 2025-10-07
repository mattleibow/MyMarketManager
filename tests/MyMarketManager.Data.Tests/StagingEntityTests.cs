using Microsoft.EntityFrameworkCore;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Tests;

public class StagingEntityTests : SqliteTestBase
{
    [Fact]
    public async Task StagingBatch_CanContainMultiplePurchaseOrders()
    {
        // Arrange
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = "Test Supplier"
        };
        Context.Suppliers.Add(supplier);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var batch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            SupplierId = supplier.Id,
            UploadDate = DateTimeOffset.UtcNow,
            FileHash = "abc123",
            Status = ProcessingStatus.Pending
        };
        Context.StagingBatches.Add(batch);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var order1 = new StagingPurchaseOrder
        {
            Id = Guid.NewGuid(),
            StagingBatchId = batch.Id,
            SupplierReference = "REF001",
            OrderDate = DateTimeOffset.UtcNow,
            RawData = "{\"order\":1}",
            IsImported = false
        };
        var order2 = new StagingPurchaseOrder
        {
            Id = Guid.NewGuid(),
            StagingBatchId = batch.Id,
            SupplierReference = "REF002",
            OrderDate = DateTimeOffset.UtcNow,
            RawData = "{\"order\":2}",
            IsImported = false
        };

        Context.StagingPurchaseOrders.AddRange(order1, order2);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var batchWithOrders = await Context.StagingBatches
            .Include(b => b.StagingPurchaseOrders)
            .FirstAsync(b => b.Id == batch.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, batchWithOrders.StagingPurchaseOrders.Count);
    }

    [Fact]
    public async Task StagingPurchaseOrder_CanHaveMultipleItems()
    {
        // Arrange
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = "Test Supplier"
        };
        Context.Suppliers.Add(supplier);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var batch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            SupplierId = supplier.Id,
            UploadDate = DateTimeOffset.UtcNow,
            FileHash = "xyz789",
            Status = ProcessingStatus.Pending
        };
        Context.StagingBatches.Add(batch);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var order = new StagingPurchaseOrder
        {
            Id = Guid.NewGuid(),
            StagingBatchId = batch.Id,
            SupplierReference = "REF003",
            OrderDate = DateTimeOffset.UtcNow,
            RawData = "{\"order\":3}",
            IsImported = false
        };
        Context.StagingPurchaseOrders.Add(order);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var item1 = new StagingPurchaseOrderItem
        {
            Id = Guid.NewGuid(),
            StagingPurchaseOrderId = order.Id,
            Name = "Staging Item 1",
            Quantity = 10,
            ListedUnitPrice = 100.00m,
            ActualUnitPrice = 95.00m,
            RawData = "{\"item\":1}",
            IsImported = false,
            Status = CandidateStatus.Pending
        };
        var item2 = new StagingPurchaseOrderItem
        {
            Id = Guid.NewGuid(),
            StagingPurchaseOrderId = order.Id,
            Name = "Staging Item 2",
            Quantity = 5,
            ListedUnitPrice = 50.00m,
            ActualUnitPrice = 48.00m,
            RawData = "{\"item\":2}",
            IsImported = false,
            Status = CandidateStatus.Pending
        };

        Context.StagingPurchaseOrderItems.AddRange(item1, item2);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var orderWithItems = await Context.StagingPurchaseOrders
            .Include(o => o.Items)
            .FirstAsync(o => o.Id == order.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, orderWithItems.Items.Count);
        Assert.Equal(15, orderWithItems.Items.Sum(i => i.Quantity));
    }

    [Fact]
    public async Task StagingSaleItem_CanLinkToProduct()
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Quality = ProductQuality.Good
        };
        Context.Products.Add(product);

        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = "Test Supplier"
        };
        Context.Suppliers.Add(supplier);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var batch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            SupplierId = supplier.Id,
            UploadDate = DateTimeOffset.UtcNow,
            FileHash = "sale123",
            Status = ProcessingStatus.Pending
        };
        Context.StagingBatches.Add(batch);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sale = new StagingSale
        {
            Id = Guid.NewGuid(),
            StagingBatchId = batch.Id,
            SaleDate = DateTimeOffset.UtcNow,
            RawData = "{\"sale\":1}",
            IsImported = false
        };
        Context.StagingSales.Add(sale);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var saleItem = new StagingSaleItem
        {
            Id = Guid.NewGuid(),
            StagingSaleId = sale.Id,
            ProductId = product.Id,
            ProductDescription = "Test Product",
            SaleDate = DateTimeOffset.UtcNow,
            Price = 50.00m,
            Quantity = 2,
            RawData = "{\"item\":1}",
            IsImported = false,
            Status = CandidateStatus.Linked
        };
        Context.StagingSaleItems.Add(saleItem);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var linkedItem = await Context.StagingSaleItems
            .Include(i => i.Product)
            .FirstAsync(i => i.Id == saleItem.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(linkedItem.Product);
        Assert.Equal("Test Product", linkedItem.Product.Name);
        Assert.Equal(CandidateStatus.Linked, linkedItem.Status);
    }
}
