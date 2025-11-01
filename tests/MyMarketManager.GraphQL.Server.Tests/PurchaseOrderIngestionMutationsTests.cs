using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
using MyMarketManager.GraphQL.Server;
using MyMarketManager.Tests.Shared;

namespace MyMarketManager.GraphQL.Server.Tests;

[Trait(TestCategories.Key, TestCategories.Values.GraphQL)]
public class PurchaseOrderIngestionMutationsTests(ITestOutputHelper outputHelper) : SqliteTestBase(outputHelper, createSchema: true)
{
    private readonly ILogger<PurchaseOrderIngestionMutations> _logger = outputHelper.ToLogger<PurchaseOrderIngestionMutations>();
    private PurchaseOrderIngestionMutations Mutations => new();

    [Fact]
    public async Task SubmitCookies_WithValidInput_ShouldCreateStagingBatch()
    {
        // Arrange
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Test Supplier" };
        Context.Suppliers.Add(supplier);
        await Context.SaveChangesAsync(Cancel);

        var input = new SubmitCookiesInput(
            SupplierId: supplier.Id,
            ProcessorName: "Shein",
            CookieJson: """{"cookie1": "value1", "cookie2": "value2"}"""
        );

        // Act
        var result = await Mutations.SubmitCookies(input, Context, _logger, Cancel);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.BatchId);
        Assert.Contains("successfully", result.Message);
        Assert.Null(result.Error);

        // Verify batch was created in database
        var batch = await Context.StagingBatches.FindAsync(result.BatchId);
        Assert.NotNull(batch);
        Assert.Equal(StagingBatchType.WebScrape, batch.BatchType);
        Assert.Equal("Shein", batch.BatchProcessorName);
        Assert.Equal(supplier.Id, batch.SupplierId);
        Assert.Equal(ProcessingStatus.Queued, batch.Status);
        Assert.NotNull(batch.FileHash);
        Assert.Equal(input.CookieJson, batch.FileContents);
    }

    [Fact]
    public async Task SubmitCookies_WithEmptyCookieJson_ShouldReturnError()
    {
        // Arrange
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Test Supplier" };
        Context.Suppliers.Add(supplier);
        await Context.SaveChangesAsync(Cancel);

        var input = new SubmitCookiesInput(
            SupplierId: supplier.Id,
            ProcessorName: "Shein",
            CookieJson: ""
        );

        // Act
        var result = await Mutations.SubmitCookies(input, Context, _logger, Cancel);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.BatchId);
        Assert.Equal("Cookie JSON is required", result.Error);
    }

    [Fact]
    public async Task SubmitCookies_WithEmptyProcessorName_ShouldReturnError()
    {
        // Arrange
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Test Supplier" };
        Context.Suppliers.Add(supplier);
        await Context.SaveChangesAsync(Cancel);

        var input = new SubmitCookiesInput(
            SupplierId: supplier.Id,
            ProcessorName: "",
            CookieJson: """{"cookie1": "value1"}"""
        );

        // Act
        var result = await Mutations.SubmitCookies(input, Context, _logger, Cancel);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.BatchId);
        Assert.Equal("Processor name is required", result.Error);
    }

    [Fact]
    public async Task SubmitCookies_WithInvalidJson_ShouldReturnError()
    {
        // Arrange
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Test Supplier" };
        Context.Suppliers.Add(supplier);
        await Context.SaveChangesAsync(Cancel);

        var input = new SubmitCookiesInput(
            SupplierId: supplier.Id,
            ProcessorName: "Shein",
            CookieJson: "not valid json {{"
        );

        // Act
        var result = await Mutations.SubmitCookies(input, Context, _logger, Cancel);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.BatchId);
        Assert.Equal("Invalid JSON format", result.Error);
    }

    [Fact]
    public async Task SubmitCookies_WithNonExistentSupplier_ShouldReturnError()
    {
        // Arrange
        var nonExistentSupplierId = Guid.NewGuid();
        var input = new SubmitCookiesInput(
            SupplierId: nonExistentSupplierId,
            ProcessorName: "Shein",
            CookieJson: """{"cookie1": "value1"}"""
        );

        // Act
        var result = await Mutations.SubmitCookies(input, Context, _logger, Cancel);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.BatchId);
        Assert.Equal("Supplier not found", result.Error);
    }

    [Fact]
    public async Task SubmitCookies_ShouldComputeUniqueHashForDifferentCookies()
    {
        // Arrange
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Test Supplier" };
        Context.Suppliers.Add(supplier);
        await Context.SaveChangesAsync(Cancel);

        var input1 = new SubmitCookiesInput(
            SupplierId: supplier.Id,
            ProcessorName: "Shein",
            CookieJson: """{"cookie1": "value1"}"""
        );

        var input2 = new SubmitCookiesInput(
            SupplierId: supplier.Id,
            ProcessorName: "Shein",
            CookieJson: """{"cookie1": "value2"}"""
        );

        // Act
        var result1 = await Mutations.SubmitCookies(input1, Context, _logger, Cancel);
        var result2 = await Mutations.SubmitCookies(input2, Context, _logger, Cancel);

        // Assert
        Assert.True(result1.Success);
        Assert.True(result2.Success);

        var batch1 = await Context.StagingBatches.FindAsync(result1.BatchId);
        var batch2 = await Context.StagingBatches.FindAsync(result2.BatchId);

        Assert.NotNull(batch1);
        Assert.NotNull(batch2);
        Assert.NotEqual(batch1.FileHash, batch2.FileHash);
    }
}
