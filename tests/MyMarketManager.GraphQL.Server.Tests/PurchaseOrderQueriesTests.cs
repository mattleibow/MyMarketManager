using System.Text.Json;
using System.Text.Json.Serialization;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
using MyMarketManager.Tests.Shared;

namespace MyMarketManager.GraphQL.Server.Tests;

[Trait(TestCategories.Key, TestCategories.Values.GraphQL)]
public class PurchaseOrderQueriesTests(ITestOutputHelper outputHelper) : GraphQLTestBase(outputHelper, createSchema: true)
{
    [Fact]
    public async Task GetPurchaseOrders_ShouldReturnAllOrders_OrderedByDateDescending()
    {
        // Arrange
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Test Supplier" };
        Context.Suppliers.Add(supplier);

        var po1 = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            SupplierId = supplier.Id,
            OrderDate = DateTimeOffset.UtcNow.AddDays(-2),
            Status = ProcessingStatus.Completed
        };
        var po2 = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            SupplierId = supplier.Id,
            OrderDate = DateTimeOffset.UtcNow,
            Status = ProcessingStatus.Started
        };

        Context.PurchaseOrders.AddRange(po1, po2);
        await Context.SaveChangesAsync(Cancel);

        // Act
        var result = await ExecuteQueryAsync<PurchaseOrdersResponse>("""
            query {
                purchaseOrders(order: { orderDate: DESC }) {
                    id
                    supplierId
                    orderDate
                    status
                    items {
                        id
                        name
                        quantity
                    }
                }
            }
            """);

        // Assert
        Assert.Equal(2, result.PurchaseOrders.Count);
        Assert.Equal(po2.Id, result.PurchaseOrders[0].Id); // Most recent first
        Assert.Equal(po1.Id, result.PurchaseOrders[1].Id);
    }

    [Fact]
    public async Task GetPurchaseOrders_ShouldIncludeItemCount()
    {
        // Arrange
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Test Supplier" };
        Context.Suppliers.Add(supplier);

        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            SupplierId = supplier.Id,
            OrderDate = DateTimeOffset.UtcNow,
            Status = ProcessingStatus.Completed
        };
        Context.PurchaseOrders.Add(po);

        var item1 = new PurchaseOrderItem
        {
            Id = Guid.NewGuid(),
            PurchaseOrderId = po.Id,
            Name = "Item 1",
            Quantity = 1,
            ListedUnitPrice = 10m,
            ActualUnitPrice = 10m
        };
        var item2 = new PurchaseOrderItem
        {
            Id = Guid.NewGuid(),
            PurchaseOrderId = po.Id,
            Name = "Item 2",
            Quantity = 2,
            ListedUnitPrice = 20m,
            ActualUnitPrice = 20m
        };
        Context.PurchaseOrderItems.AddRange(item1, item2);
        await Context.SaveChangesAsync(Cancel);

        // Act
        var result = await ExecuteQueryAsync<PurchaseOrdersResponse>("""
            query {
                purchaseOrders {
                    id
                    items {
                        id
                        name
                        quantity
                    }
                }
            }
            """);

        // Assert
        Assert.Single(result.PurchaseOrders);
        Assert.Equal(2, result.PurchaseOrders[0].Items.Count);
    }

    [Fact]
    public async Task GetPurchaseOrderById_WithValidId_ShouldReturnDetailWithItems()
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

        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            SupplierId = supplier.Id,
            OrderDate = DateTimeOffset.UtcNow,
            Status = ProcessingStatus.Completed,
            ShippingFees = 5m,
            ImportFees = 3m
        };
        Context.PurchaseOrders.Add(po);

        var item = new PurchaseOrderItem
        {
            Id = Guid.NewGuid(),
            PurchaseOrderId = po.Id,
            ProductId = product.Id,
            Name = "Test Item",
            Quantity = 5,
            ListedUnitPrice = 10m,
            ActualUnitPrice = 9m,
            AllocatedUnitOverhead = 1m
        };
        Context.PurchaseOrderItems.Add(item);
        await Context.SaveChangesAsync(Cancel);

        // Act
        var result = await ExecuteQueryAsync<PurchaseOrderByIdResponse>($$"""
            query {
                purchaseOrderById(id: "{{po.Id}}") {
                    id
                    orderDate
                    status
                    shippingFees
                    importFees
                    supplier {
                        name
                    }
                    items {
                        name
                        quantity
                        product {
                            name
                        }
                    }
                }
            }
            """);

        // Assert
        Assert.NotNull(result.PurchaseOrderById);
        Assert.Equal(po.Id, result.PurchaseOrderById.Id);
        Assert.Equal(supplier.Name, result.PurchaseOrderById.Supplier.Name);
        Assert.Equal(5m, result.PurchaseOrderById.ShippingFees);
        Assert.Equal(3m, result.PurchaseOrderById.ImportFees);
        Assert.Single(result.PurchaseOrderById.Items);
        Assert.Equal("Test Item", result.PurchaseOrderById.Items.First().Name);
        Assert.Equal(product.Name, result.PurchaseOrderById.Items.First().Product!.Name);
        Assert.Equal(5, result.PurchaseOrderById.Items.First().Quantity);
    }

    [Fact]
    public async Task GetPurchaseOrderById_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await ExecuteQueryAsync<PurchaseOrderByIdResponse>($$"""
            query {
                purchaseOrderById(id: "{{nonExistentId}}") {
                    id
                }
            }
            """);

        // Assert
        Assert.Null(result.PurchaseOrderById);
    }

    private record PurchaseOrdersResponse(List<PurchaseOrderDto> PurchaseOrders);
    private record PurchaseOrderByIdResponse(PurchaseOrderDetailDto? PurchaseOrderById);
    private record PurchaseOrderDto(Guid Id, List<PurchaseOrderItemDto> Items);
    private record PurchaseOrderItemDto(Guid Id, string Name, int Quantity);
    private record PurchaseOrderDetailDto(Guid Id, DateTimeOffset OrderDate, ProcessingStatus Status, decimal? ShippingFees, decimal? ImportFees, SupplierDto Supplier, List<PurchaseOrderItemDetailDto> Items);
    private record SupplierDto(string Name);
    private record PurchaseOrderItemDetailDto(string Name, int Quantity, ProductDto? Product);
    private record ProductDto(string Name);
}
