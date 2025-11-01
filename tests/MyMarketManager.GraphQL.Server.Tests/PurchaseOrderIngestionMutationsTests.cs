using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
using MyMarketManager.Tests.Shared;

namespace MyMarketManager.GraphQL.Server.Tests;

[Trait(TestCategories.Key, TestCategories.Values.GraphQL)]
public class PurchaseOrderIngestionMutationsTests(ITestOutputHelper outputHelper) : GraphQLTestBase(outputHelper, createSchema: true)
{
    [Fact]
    public async Task SubmitCookies_WithValidInput_ShouldCreateStagingBatch()
    {
        // Arrange
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Test Supplier" };
        Context.Suppliers.Add(supplier);
        await Context.SaveChangesAsync(Cancel);

        var cookieJson = """{"cookie1": "value1", "cookie2": "value2"}""";

        // Act
        var result = await ExecuteQueryAsync<SubmitCookiesResponse>($$"""
            mutation {
                submitCookies(input: {
                    supplierId: "{{supplier.Id}}"
                    processorName: "Shein"
                    cookieJson: "{{cookieJson.Replace("\"", "\\\"")}}"
                }) {
                    success
                    batchId
                    message
                    error
                }
            }
        """);

        // Assert
        Assert.True(result.SubmitCookies.Success);
        Assert.NotNull(result.SubmitCookies.BatchId);
        Assert.Contains("successfully", result.SubmitCookies.Message);
        Assert.Null(result.SubmitCookies.Error);

        // Verify batch was created in database
        var batch = await Context.StagingBatches.FindAsync(result.SubmitCookies.BatchId);
        Assert.NotNull(batch);
        Assert.Equal(StagingBatchType.WebScrape, batch.BatchType);
        Assert.Equal("Shein", batch.BatchProcessorName);
        Assert.Equal(supplier.Id, batch.SupplierId);
        Assert.Equal(ProcessingStatus.Queued, batch.Status);
        Assert.NotNull(batch.FileHash);
        Assert.Equal(cookieJson, batch.FileContents);
    }

    [Fact]
    public async Task SubmitCookies_WithEmptyCookieJson_ShouldReturnError()
    {
        // Arrange
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Test Supplier" };
        Context.Suppliers.Add(supplier);
        await Context.SaveChangesAsync(Cancel);

        // Act
        var result = await ExecuteQueryAsync<SubmitCookiesResponse>($$"""
            mutation {
                submitCookies(input: {
                    supplierId: "{{supplier.Id}}"
                    processorName: "Shein"
                    cookieJson: ""
                }) {
                    success
                    batchId
                    error
                }
            }
        """);

        // Assert
        Assert.False(result.SubmitCookies.Success);
        Assert.Null(result.SubmitCookies.BatchId);
        Assert.Equal("Cookie JSON is required", result.SubmitCookies.Error);
    }

    [Fact]
    public async Task SubmitCookies_WithEmptyProcessorName_ShouldReturnError()
    {
        // Arrange
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Test Supplier" };
        Context.Suppliers.Add(supplier);
        await Context.SaveChangesAsync(Cancel);

        var cookieJson = """{"cookie1": "value1"}""";

        // Act
        var result = await ExecuteQueryAsync<SubmitCookiesResponse>($$"""
            mutation {
                submitCookies(input: {
                    supplierId: "{{supplier.Id}}"
                    processorName: ""
                    cookieJson: "{{cookieJson.Replace("\"", "\\\"")}}"
                }) {
                    success
                    batchId
                    error
                }
            }
        """);

        // Assert
        Assert.False(result.SubmitCookies.Success);
        Assert.Null(result.SubmitCookies.BatchId);
        Assert.Equal("Processor name is required", result.SubmitCookies.Error);
    }

    [Fact]
    public async Task SubmitCookies_WithInvalidJson_ShouldReturnError()
    {
        // Arrange
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Test Supplier" };
        Context.Suppliers.Add(supplier);
        await Context.SaveChangesAsync(Cancel);

        // Act - Using invalid JSON with unmatched braces
        var result = await ExecuteQueryAsync<SubmitCookiesResponse>($$$"""
            mutation {
                submitCookies(input: {
                    supplierId: "{{{supplier.Id}}}"
                    processorName: "Shein"
                    cookieJson: "not valid json {{"
                }) {
                    success
                    batchId
                    error
                }
            }
        """);

        // Assert
        Assert.False(result.SubmitCookies.Success);
        Assert.Null(result.SubmitCookies.BatchId);
        Assert.Equal("Invalid JSON format", result.SubmitCookies.Error);
    }

    [Fact]
    public async Task SubmitCookies_WithNonExistentSupplier_ShouldReturnError()
    {
        // Arrange
        var nonExistentSupplierId = Guid.NewGuid();
        var cookieJson = """{"cookie1": "value1"}""";

        // Act
        var result = await ExecuteQueryAsync<SubmitCookiesResponse>($$"""
            mutation {
                submitCookies(input: {
                    supplierId: "{{nonExistentSupplierId}}"
                    processorName: "Shein"
                    cookieJson: "{{cookieJson.Replace("\"", "\\\"")}}"
                }) {
                    success
                    batchId
                    error
                }
            }
        """);

        // Assert
        Assert.False(result.SubmitCookies.Success);
        Assert.Null(result.SubmitCookies.BatchId);
        Assert.Equal("Supplier not found", result.SubmitCookies.Error);
    }

    [Fact]
    public async Task SubmitCookies_ShouldComputeUniqueHashForDifferentCookies()
    {
        // Arrange
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Test Supplier" };
        Context.Suppliers.Add(supplier);
        await Context.SaveChangesAsync(Cancel);

        var cookieJson1 = """{"cookie1": "value1"}""";
        var cookieJson2 = """{"cookie1": "value2"}""";

        // Act
        var result1 = await ExecuteQueryAsync<SubmitCookiesResponse>($$"""
            mutation {
                submitCookies(input: {
                    supplierId: "{{supplier.Id}}"
                    processorName: "Shein"
                    cookieJson: "{{cookieJson1.Replace("\"", "\\\"")}}"
                }) {
                    success
                    batchId
                }
            }
        """);
        
        var result2 = await ExecuteQueryAsync<SubmitCookiesResponse>($$"""
            mutation {
                submitCookies(input: {
                    supplierId: "{{supplier.Id}}"
                    processorName: "Shein"
                    cookieJson: "{{cookieJson2.Replace("\"", "\\\"")}}"
                }) {
                    success
                    batchId
                }
            }
        """);

        // Assert
        Assert.True(result1.SubmitCookies.Success);
        Assert.True(result2.SubmitCookies.Success);

        var batch1 = await Context.StagingBatches.FindAsync(result1.SubmitCookies.BatchId);
        var batch2 = await Context.StagingBatches.FindAsync(result2.SubmitCookies.BatchId);

        Assert.NotNull(batch1);
        Assert.NotNull(batch2);
        Assert.NotEqual(batch1.FileHash, batch2.FileHash);
    }

    private record SubmitCookiesResponse(SubmitCookiesPayloadDto SubmitCookies);
    private record SubmitCookiesPayloadDto(bool Success, Guid? BatchId, string? Message, string? Error);
}

