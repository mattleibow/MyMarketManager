using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
using MyMarketManager.Data.Services;

namespace MyMarketManager.Data.Tests.Services;

public class BatchIngestionProcessorTests(ITestOutputHelper outputHelper) : SqlServerTestBase(createSchema: true)
{
    private readonly ILogger<BatchIngestionProcessor> _logger = outputHelper.ToLogger<BatchIngestionProcessor>();

    [Fact]
    public async Task ProcessPendingBatchesAsync_WithNoPendingBatches_ReturnsZero()
    {
        // Arrange
        var processor = new BatchIngestionProcessor(Context, _logger);

        // Act
        var result = await processor.ProcessPendingBatchesAsync(MockDownloadAsync, Cancel);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ProcessPendingBatchesAsync_WithPendingBatch_ProcessesAndReturnsCount()
    {
        // Arrange
        var supplier = await CreateSupplierAsync();
        var batch = await CreatePendingBatchAsync(supplier.Id, "https://blob/test.zip");
        
        var processor = new BatchIngestionProcessor(Context, _logger);

        // Act
        var result = await processor.ProcessPendingBatchesAsync(MockDownloadAsync, Cancel);

        // Assert
        Assert.Equal(1, result);

        // Verify batch was marked as complete
        var updatedBatch = await Context.StagingBatches.FindAsync(batch.Id);
        Assert.NotNull(updatedBatch);
        Assert.Equal(ProcessingStatus.Complete, updatedBatch.Status);
        Assert.Contains("Processed on", updatedBatch.Notes);
    }

    [Fact]
    public async Task ProcessPendingBatchesAsync_WithMultiplePendingBatches_ProcessesAll()
    {
        // Arrange
        var supplier = await CreateSupplierAsync();
        var batch1 = await CreatePendingBatchAsync(supplier.Id, "https://blob/test1.zip");
        var batch2 = await CreatePendingBatchAsync(supplier.Id, "https://blob/test2.zip");
        var batch3 = await CreatePendingBatchAsync(supplier.Id, "https://blob/test3.zip");
        
        var processor = new BatchIngestionProcessor(Context, _logger);

        // Act
        var result = await processor.ProcessPendingBatchesAsync(MockDownloadAsync, Cancel);

        // Assert
        Assert.Equal(3, result);

        // Verify all batches were marked as complete
        var allBatches = await Context.StagingBatches.ToListAsync(Cancel);
        Assert.All(allBatches, b => Assert.Equal(ProcessingStatus.Complete, b.Status));
    }

    [Fact]
    public async Task ProcessPendingBatchesAsync_SkipsCompletedBatches()
    {
        // Arrange
        var supplier = await CreateSupplierAsync();
        var pendingBatch = await CreatePendingBatchAsync(supplier.Id, "https://blob/pending.zip");
        var completedBatch = await CreateBatchAsync(supplier.Id, "https://blob/completed.zip", ProcessingStatus.Complete);
        
        var processor = new BatchIngestionProcessor(Context, _logger);

        // Act
        var result = await processor.ProcessPendingBatchesAsync(MockDownloadAsync, Cancel);

        // Assert - Only pending batch should be processed
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ProcessBatchAsync_WithValidBatch_MarksAsComplete()
    {
        // Arrange
        var supplier = await CreateSupplierAsync();
        var batch = await CreatePendingBatchAsync(supplier.Id, "https://blob/test.zip");
        
        var processor = new BatchIngestionProcessor(Context, _logger);

        // Act
        await processor.ProcessBatchAsync(batch, MockDownloadAsync, Cancel);

        // Assert
        var updatedBatch = await Context.StagingBatches.FindAsync(batch.Id);
        Assert.NotNull(updatedBatch);
        Assert.Equal(ProcessingStatus.Complete, updatedBatch.Status);
        Assert.Contains("Processed on", updatedBatch.Notes);
    }

    [Fact]
    public async Task ProcessBatchAsync_WithNoBlobUrl_MarksAsPartial()
    {
        // Arrange
        var supplier = await CreateSupplierAsync();
        var batch = await CreatePendingBatchAsync(supplier.Id, blobUrl: null);
        
        var processor = new BatchIngestionProcessor(Context, _logger);

        // Act
        await processor.ProcessBatchAsync(batch, MockDownloadAsync, Cancel);

        // Assert
        var updatedBatch = await Context.StagingBatches.FindAsync(batch.Id);
        Assert.NotNull(updatedBatch);
        Assert.Equal(ProcessingStatus.Partial, updatedBatch.Status);
        Assert.Equal("No blob storage URL provided", updatedBatch.Notes);
    }

    [Fact]
    public async Task ProcessBatchAsync_WhenDownloadFails_MarksAsPartialWithError()
    {
        // Arrange
        var supplier = await CreateSupplierAsync();
        var batch = await CreatePendingBatchAsync(supplier.Id, "https://blob/test.zip");
        
        var processor = new BatchIngestionProcessor(Context, _logger);

        // Act & Assert - Exception should be thrown by ProcessBatchAsync
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await processor.ProcessBatchAsync(
                batch, 
                (url, ct) => throw new InvalidOperationException("Download failed"),
                Cancel);
        });
    }

    [Fact]
    public async Task ProcessPendingBatchesAsync_WhenOneBatchFails_ContinuesWithOthers()
    {
        // Arrange
        var supplier = await CreateSupplierAsync();
        var batch1 = await CreatePendingBatchAsync(supplier.Id, "https://blob/test1.zip");
        var batch2 = await CreatePendingBatchAsync(supplier.Id, "fail"); // Will trigger error
        var batch3 = await CreatePendingBatchAsync(supplier.Id, "https://blob/test3.zip");
        
        var processor = new BatchIngestionProcessor(Context, _logger);

        // Act
        var result = await processor.ProcessPendingBatchesAsync(
            (url, ct) => url == "fail" 
                ? throw new InvalidOperationException("Download failed")
                : MockDownloadAsync(url, ct),
            Cancel);

        // Assert - Should process 2 successful batches
        Assert.Equal(2, result);

        // Verify failed batch is marked as partial
        var failedBatch = await Context.StagingBatches.FindAsync(batch2.Id);
        Assert.NotNull(failedBatch);
        Assert.Equal(ProcessingStatus.Partial, failedBatch.Status);
        Assert.Contains("Processing error", failedBatch.Notes);
    }

    [Fact]
    public async Task ProcessBatchAsync_DownloadsAndReadsFileContent()
    {
        // Arrange
        var supplier = await CreateSupplierAsync();
        var batch = await CreatePendingBatchAsync(supplier.Id, "https://blob/test.zip");
        var testContent = "Test file content"u8.ToArray();
        
        var processor = new BatchIngestionProcessor(Context, _logger);
        var downloadCalled = false;

        // Act
        await processor.ProcessBatchAsync(
            batch,
            async (url, ct) =>
            {
                downloadCalled = true;
                await Task.CompletedTask;
                return new MemoryStream(testContent);
            },
            Cancel);

        // Assert
        Assert.True(downloadCalled, "Download function should be called");
        
        var updatedBatch = await Context.StagingBatches.FindAsync(batch.Id);
        Assert.NotNull(updatedBatch);
        Assert.Equal(ProcessingStatus.Complete, updatedBatch.Status);
    }

    // Helper methods

    private async Task<Supplier> CreateSupplierAsync()
    {
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = "Test Supplier",
            ContactInfo = "test@example.com"
        };
        Context.Suppliers.Add(supplier);
        await Context.SaveChangesAsync(Cancel);
        return supplier;
    }

    private async Task<StagingBatch> CreatePendingBatchAsync(Guid supplierId, string? blobUrl)
    {
        return await CreateBatchAsync(supplierId, blobUrl, ProcessingStatus.Pending);
    }

    private async Task<StagingBatch> CreateBatchAsync(Guid supplierId, string? blobUrl, ProcessingStatus status)
    {
        var batch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            SupplierId = supplierId,
            UploadDate = DateTimeOffset.UtcNow,
            FileHash = Guid.NewGuid().ToString("N"),
            BlobStorageUrl = blobUrl,
            BatchType = BatchType.SupplierData,
            Status = status,
            Notes = "Test batch"
        };
        Context.StagingBatches.Add(batch);
        await Context.SaveChangesAsync(Cancel);
        return batch;
    }

    private static Task<Stream> MockDownloadAsync(string url, CancellationToken cancellationToken)
    {
        var content = "Mock file content"u8.ToArray();
        return Task.FromResult<Stream>(new MemoryStream(content));
    }
}
