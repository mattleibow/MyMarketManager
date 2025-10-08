using Microsoft.EntityFrameworkCore;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Tests;

public class EntityRelationshipTests : SqliteTestBase
{
    [Fact]
    public async Task Supplier_CanHaveMultiplePurchaseOrders()
    {
        // Arrange
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = "Test Supplier"
        };
        Context.Suppliers.Add(supplier);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var order1 = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            SupplierId = supplier.Id,
            OrderDate = DateTimeOffset.UtcNow,
            Status = ProcessingStatus.Pending
        };
        var order2 = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            SupplierId = supplier.Id,
            OrderDate = DateTimeOffset.UtcNow,
            Status = ProcessingStatus.Complete
        };

        Context.PurchaseOrders.AddRange(order1, order2);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var supplierWithOrders = await Context.Suppliers
            .Include(s => s.PurchaseOrders)
            .FirstAsync(s => s.Id == supplier.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, supplierWithOrders.PurchaseOrders.Count);
    }

    [Fact]
    public async Task PurchaseOrder_CanHaveMultipleItems()
    {
        // Arrange
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = "Test Supplier"
        };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Quality = ProductQuality.Good
        };
        Context.Suppliers.Add(supplier);
        Context.Products.Add(product);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var order = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            SupplierId = supplier.Id,
            OrderDate = DateTimeOffset.UtcNow,
            Status = ProcessingStatus.Pending
        };
        Context.PurchaseOrders.Add(order);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var item1 = new PurchaseOrderItem
        {
            Id = Guid.NewGuid(),
            PurchaseOrderId = order.Id,
            ProductId = product.Id,
            Name = "Item 1",
            Quantity = 10,
            ListedUnitPrice = 100.00m,
            ActualUnitPrice = 95.00m,
            AllocatedUnitOverhead = 5.00m,
            TotalUnitCost = 100.00m
        };
        var item2 = new PurchaseOrderItem
        {
            Id = Guid.NewGuid(),
            PurchaseOrderId = order.Id,
            ProductId = product.Id,
            Name = "Item 2",
            Quantity = 5,
            ListedUnitPrice = 50.00m,
            ActualUnitPrice = 48.00m,
            AllocatedUnitOverhead = 2.00m,
            TotalUnitCost = 50.00m
        };

        Context.PurchaseOrderItems.AddRange(item1, item2);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var orderWithItems = await Context.PurchaseOrders
            .Include(o => o.Items)
            .FirstAsync(o => o.Id == order.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, orderWithItems.Items.Count);
        Assert.Equal(15, orderWithItems.Items.Sum(i => i.Quantity));
    }

    [Fact]
    public async Task Product_CanHaveMultiplePhotos()
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Quality = ProductQuality.Excellent
        };
        Context.Products.Add(product);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var photo1 = new ProductPhoto
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Url = "https://example.com/photo1.jpg",
            Caption = "Front view"
        };
        var photo2 = new ProductPhoto
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Url = "https://example.com/photo2.jpg",
            Caption = "Side view"
        };

        Context.ProductPhotos.AddRange(photo1, photo2);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var productWithPhotos = await Context.Products
            .Include(p => p.Photos)
            .FirstAsync(p => p.Id == product.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, productWithPhotos.Photos.Count);
    }

    [Fact]
    public async Task MarketEvent_CanHaveMultipleReconciledSales()
    {
        // Arrange
        var marketEvent = new MarketEvent
        {
            Id = Guid.NewGuid(),
            Name = "Weekend Market",
            Date = DateTimeOffset.UtcNow
        };
        var product1 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Product 1",
            Quality = ProductQuality.Good
        };
        var product2 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Product 2",
            Quality = ProductQuality.Fair
        };

        Context.MarketEvents.Add(marketEvent);
        Context.Products.AddRange(product1, product2);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sale1 = new ReconciledSale
        {
            Id = Guid.NewGuid(),
            MarketEventId = marketEvent.Id,
            ProductId = product1.Id,
            Quantity = 5,
            SalePrice = 50.00m
        };
        var sale2 = new ReconciledSale
        {
            Id = Guid.NewGuid(),
            MarketEventId = marketEvent.Id,
            ProductId = product2.Id,
            Quantity = 3,
            SalePrice = 30.00m
        };

        Context.ReconciledSales.AddRange(sale1, sale2);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var eventWithSales = await Context.MarketEvents
            .Include(e => e.ReconciledSales)
            .FirstAsync(e => e.Id == marketEvent.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, eventWithSales.ReconciledSales.Count);
        Assert.Equal(80.00m, eventWithSales.ReconciledSales.Sum(s => s.SalePrice));
    }
}
