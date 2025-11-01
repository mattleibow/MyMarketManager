using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
using MyMarketManager.Tests.Shared;

namespace MyMarketManager.GraphQL.Server.Tests;

[Trait(TestCategories.Key, TestCategories.Values.GraphQL)]
public class StagingBatchQueriesTests(ITestOutputHelper outputHelper) : GraphQLTestBase(outputHelper, createSchema: true)
{
    [Fact]
    public async Task GetStagingBatches_ShouldReturnAllBatches_OrderedByStartDateDescending()
    {
        // Arrange
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Test Supplier" };
        Context.Suppliers.Add(supplier);

        var batch1 = new StagingBatch
        {
            Id = Guid.NewGuid(),
            BatchType = StagingBatchType.WebScrape,
            SupplierId = supplier.Id,
            StartedAt = DateTimeOffset.UtcNow.AddHours(-2),
            Status = ProcessingStatus.Completed
        };
        var batch2 = new StagingBatch
        {
            Id = Guid.NewGuid(),
            BatchType = StagingBatchType.WebScrape,
            SupplierId = supplier.Id,
            StartedAt = DateTimeOffset.UtcNow,
            Status = ProcessingStatus.Started
        };

        Context.StagingBatches.AddRange(batch1, batch2);
        await Context.SaveChangesAsync(Cancel);

        // Act
        var result = await ExecuteQueryAsync<StagingBatchesResponse>(@"
            query {
                stagingBatches(order: { startedAt: DESC }) {
                    id
                    startedAt
                }
            }
        ");

        // Assert
        Assert.Equal(2, result.StagingBatches.Count);
        Assert.Equal(batch2.Id, result.StagingBatches[0].Id); // Most recent first
        Assert.Equal(batch1.Id, result.StagingBatches[1].Id);
    }

    [Fact]
    public async Task GetStagingBatches_ShouldIncludeOrderAndItemCounts()
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

        var order1 = new StagingPurchaseOrder
        {
            Id = Guid.NewGuid(),
            StagingBatchId = batch.Id,
            OrderDate = DateTimeOffset.UtcNow,
            Status = ProcessingStatus.Completed
        };
        var order2 = new StagingPurchaseOrder
        {
            Id = Guid.NewGuid(),
            StagingBatchId = batch.Id,
            OrderDate = DateTimeOffset.UtcNow,
            Status = ProcessingStatus.Completed
        };
        Context.StagingPurchaseOrders.AddRange(order1, order2);

        var item1 = new StagingPurchaseOrderItem
        {
            Id = Guid.NewGuid(),
            StagingPurchaseOrderId = order1.Id,
            Name = "Item 1",
            Quantity = 1,
            ListedUnitPrice = 10m,
            ActualUnitPrice = 10m,
            Status = CandidateStatus.Pending
        };
        var item2 = new StagingPurchaseOrderItem
        {
            Id = Guid.NewGuid(),
            StagingPurchaseOrderId = order1.Id,
            Name = "Item 2",
            Quantity = 1,
            ListedUnitPrice = 10m,
            ActualUnitPrice = 10m,
            Status = CandidateStatus.Pending
        };
        var item3 = new StagingPurchaseOrderItem
        {
            Id = Guid.NewGuid(),
            StagingPurchaseOrderId = order2.Id,
            Name = "Item 3",
            Quantity = 1,
            ListedUnitPrice = 10m,
            ActualUnitPrice = 10m,
            Status = CandidateStatus.Pending
        };
        Context.StagingPurchaseOrderItems.AddRange(item1, item2, item3);
        await Context.SaveChangesAsync(Cancel);

        // Act
        var result = await ExecuteQueryAsync<StagingBatchesResponse>(@"
            query {
                stagingBatches {
                    id
                    stagingPurchaseOrders {
                        id
                        items {
                            id
                        }
                    }
                }
            }
        ");

        // Assert
        Assert.Single(result.StagingBatches);
        Assert.Equal(2, result.StagingBatches[0].StagingPurchaseOrders.Count);
        Assert.Equal(3, result.StagingBatches[0].StagingPurchaseOrders.Sum(spo => spo.Items.Count));
    }

    [Fact]
    public async Task GetStagingBatchById_WithValidId_ShouldReturnDetailWithOrders()
    {
        // Arrange
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Test Supplier" };
        Context.Suppliers.Add(supplier);

        var batch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            BatchType = StagingBatchType.WebScrape,
            BatchProcessorName = "Shein",
            SupplierId = supplier.Id,
            StartedAt = DateTimeOffset.UtcNow,
            Status = ProcessingStatus.Completed,
            Notes = "Test batch"
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
            Name = "Test Item",
            Quantity = 1,
            ListedUnitPrice = 10m,
            ActualUnitPrice = 10m,
            Status = CandidateStatus.Pending
        };
        Context.StagingPurchaseOrderItems.Add(item);
        await Context.SaveChangesAsync(Cancel);

        // Act
        var result = await ExecuteQueryAsync<StagingBatchByIdResponse>(@"
            query($id: UUID!) {
                stagingBatchById(id: $id) {
                    id
                    batchProcessorName
                    supplier {
                        name
                    }
                    stagingPurchaseOrders {
                        id
                        supplierReference
                        items {
                            id
                        }
                    }
                }
            }
        ", new Dictionary<string, object?> { ["id"] = batch.Id });

        // Assert
        Assert.NotNull(result.StagingBatchById);
        Assert.Equal(batch.Id, result.StagingBatchById.Id);
        Assert.Equal(supplier.Name, result.StagingBatchById.Supplier.Name);
        Assert.Equal("Shein", result.StagingBatchById.BatchProcessorName);
        Assert.Single(result.StagingBatchById.StagingPurchaseOrders);
        Assert.Equal("PO-001", result.StagingBatchById.StagingPurchaseOrders[0].SupplierReference);
        Assert.Single(result.StagingBatchById.StagingPurchaseOrders[0].Items);
    }

    [Fact]
    public async Task GetStagingBatchById_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await ExecuteQueryAsync<StagingBatchByIdResponse>(@"
            query($id: UUID!) {
                stagingBatchById(id: $id) {
                    id
                }
            }
        ", new Dictionary<string, object?> { ["id"] = nonExistentId });

        // Assert
        Assert.Null(result.StagingBatchById);
    }

    private record StagingBatchesResponse(List<StagingBatchDto> StagingBatches);
    private record StagingBatchByIdResponse(StagingBatchDetailDto? StagingBatchById);
    private record StagingBatchDto(Guid Id, DateTimeOffset StartedAt, List<StagingPurchaseOrderDto> StagingPurchaseOrders);
    private record StagingBatchDetailDto(Guid Id, string? BatchProcessorName, SupplierDto Supplier, List<StagingPurchaseOrderDetailDto> StagingPurchaseOrders);
    private record StagingPurchaseOrderDto(Guid Id, List<StagingItemDto> Items);
    private record StagingPurchaseOrderDetailDto(Guid Id, string? SupplierReference, List<StagingItemDto> Items);
    private record StagingItemDto(Guid Id);
    private record SupplierDto(string Name);
}
