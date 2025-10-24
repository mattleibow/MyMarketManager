using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
using MyMarketManager.Data.Processing;
using MyMarketManager.Tests.Shared;
using MyMarketManager.WebApp.Services;
using NSubstitute;

namespace MyMarketManager.Data.Processing.Tests;

/// <summary>
/// Tests for the BatchProcessingService class.
/// </summary>
[Trait(TestCategories.Key, TestCategories.Values.Processing)]
[Trait(TestCategories.Key, TestCategories.Values.Database)]
public class BatchProcessingServiceTests(ITestOutputHelper outputHelper) : SqliteTestBase(outputHelper)
{
    private readonly IBatchProcessorFactory _mockFactory = Substitute.For<IBatchProcessorFactory>();
    private readonly ILogger<BatchProcessingService> _mockLogger = Substitute.For<ILogger<BatchProcessingService>>();

    private BatchProcessingService ProcessingService { get; set; } = null!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        
        ProcessingService = new BatchProcessingService(Context, _mockFactory, _mockLogger);
    }

    [Fact]
    public async Task ProcessBatchesAsync_WithNoQueuedBatches_CompletesWithoutProcessing()
    {
        // Act
        await ProcessingService.ProcessBatchesAsync(TestContext.Current.CancellationToken);

        // Assert
        _mockFactory.DidNotReceive().GetProcessor(Arg.Any<string>());
    }

    [Fact]
    public async Task ProcessBatchesAsync_WithQueuedBatch_ProcessesBatch()
    {
        // Arrange
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = "Test Supplier"
        };
        Context.Suppliers.Add(supplier);

        var batch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            BatchType = StagingBatchType.WebScrape,
            SupplierId = supplier.Id,
            StartedAt = DateTimeOffset.UtcNow,
            FileHash = "test-hash",
            Status = ProcessingStatus.Queued,
            BatchProcessorName = "test-processor"
        };
        Context.StagingBatches.Add(batch);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mockProcessor = Substitute.For<IBatchProcessor>();
        _mockFactory.GetProcessor("test-processor").Returns(mockProcessor);

        // Act
        await ProcessingService.ProcessBatchesAsync(TestContext.Current.CancellationToken);

        // Assert
        await mockProcessor.Received(1).ProcessBatchAsync(batch, TestContext.Current.CancellationToken);
        
        // Verify batch was marked as completed
        var updatedBatch = await Context.StagingBatches.FindAsync(batch.Id);
        Assert.NotNull(updatedBatch);
        Assert.Equal(ProcessingStatus.Completed, updatedBatch.Status);
        Assert.NotNull(updatedBatch.CompletedAt);
    }

    [Fact]
    public async Task ProcessBatchesAsync_WithMultipleQueuedBatches_ProcessesAllInOrder()
    {
        // Arrange
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = "Test Supplier"
        };
        Context.Suppliers.Add(supplier);

        var batch1 = new StagingBatch
        {
            Id = Guid.NewGuid(),
            BatchType = StagingBatchType.WebScrape,
            SupplierId = supplier.Id,
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            FileHash = "test-hash-1",
            Status = ProcessingStatus.Queued,
            BatchProcessorName = "test-processor"
        };

        var batch2 = new StagingBatch
        {
            Id = Guid.NewGuid(),
            BatchType = StagingBatchType.BlobUpload,
            SupplierId = supplier.Id,
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            FileHash = "test-hash-2",
            Status = ProcessingStatus.Queued,
            BatchProcessorName = "another-processor"
        };

        Context.StagingBatches.AddRange(batch1, batch2);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mockProcessor1 = Substitute.For<IBatchProcessor>();
        var mockProcessor2 = Substitute.For<IBatchProcessor>();
        _mockFactory.GetProcessor("test-processor").Returns(mockProcessor1);
        _mockFactory.GetProcessor("another-processor").Returns(mockProcessor2);

        // Act
        await ProcessingService.ProcessBatchesAsync(TestContext.Current.CancellationToken);

        // Assert
        await mockProcessor1.Received(1).ProcessBatchAsync(batch1, TestContext.Current.CancellationToken);
        await mockProcessor2.Received(1).ProcessBatchAsync(batch2, TestContext.Current.CancellationToken);

        // Verify both batches were marked as completed
        var updatedBatches = await Context.StagingBatches
            .Where(b => b.Id == batch1.Id || b.Id == batch2.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.All(updatedBatches, b => 
        {
            Assert.Equal(ProcessingStatus.Completed, b.Status);
            Assert.NotNull(b.CompletedAt);
        });
    }

    [Fact]
    public async Task ProcessBatchesAsync_WithBatchWithoutProcessorName_SkipsBatch()
    {
        // Arrange
        var batch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            BatchType = StagingBatchType.WebScrape,
            StartedAt = DateTimeOffset.UtcNow,
            FileHash = "test-hash",
            Status = ProcessingStatus.Queued,
            BatchProcessorName = null // No processor name
        };
        Context.StagingBatches.Add(batch);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await ProcessingService.ProcessBatchesAsync(TestContext.Current.CancellationToken);

        // Assert
        _mockFactory.DidNotReceive().GetProcessor(Arg.Any<string>());
        
        // Batch should remain queued
        var updatedBatch = await Context.StagingBatches.FindAsync(batch.Id);
        Assert.NotNull(updatedBatch);
        Assert.Equal(ProcessingStatus.Queued, updatedBatch.Status);
    }

    [Fact]
    public async Task ProcessBatchesAsync_WithUnknownProcessor_SkipsBatch()
    {
        // Arrange
        var batch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            BatchType = StagingBatchType.WebScrape,
            StartedAt = DateTimeOffset.UtcNow,
            FileHash = "test-hash",
            Status = ProcessingStatus.Queued,
            BatchProcessorName = "unknown-processor"
        };
        Context.StagingBatches.Add(batch);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mockFactory.GetProcessor("unknown-processor").Returns((IBatchProcessor?)null);

        // Act
        await ProcessingService.ProcessBatchesAsync(TestContext.Current.CancellationToken);

        // Assert
        // Batch should remain queued
        var updatedBatch = await Context.StagingBatches.FindAsync(batch.Id);
        Assert.NotNull(updatedBatch);
        Assert.Equal(ProcessingStatus.Queued, updatedBatch.Status);
    }

    [Fact]
    public async Task ProcessBatchesAsync_WhenProcessorThrows_MarksBatchAsFailed()
    {
        // Arrange
        var batch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            BatchType = StagingBatchType.WebScrape,
            StartedAt = DateTimeOffset.UtcNow,
            FileHash = "test-hash",
            Status = ProcessingStatus.Queued,
            BatchProcessorName = "failing-processor"
        };
        Context.StagingBatches.Add(batch);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mockProcessor = Substitute.For<IBatchProcessor>();
        var testException = new InvalidOperationException("Test processing error");
        mockProcessor.ProcessBatchAsync(batch, TestContext.Current.CancellationToken)
                    .Returns(Task.FromException(testException));
        _mockFactory.GetProcessor("failing-processor").Returns(mockProcessor);

        // Act
        await ProcessingService.ProcessBatchesAsync(TestContext.Current.CancellationToken);

        // Assert
        var updatedBatch = await Context.StagingBatches.FindAsync(batch.Id);
        Assert.NotNull(updatedBatch);
        Assert.Equal(ProcessingStatus.Failed, updatedBatch.Status);
        Assert.Equal("Test processing error", updatedBatch.ErrorMessage);
        Assert.Null(updatedBatch.CompletedAt);
    }

    [Fact]
    public async Task ProcessBatchesAsync_OnlyProcessesQueuedBatches()
    {
        // Arrange
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = "Test Supplier"
        };
        Context.Suppliers.Add(supplier);

        var queuedBatch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            BatchType = StagingBatchType.WebScrape,
            SupplierId = supplier.Id,
            StartedAt = DateTimeOffset.UtcNow,
            FileHash = "queued-hash",
            Status = ProcessingStatus.Queued,
            BatchProcessorName = "test-processor"
        };

        var completedBatch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            BatchType = StagingBatchType.WebScrape,
            SupplierId = supplier.Id,
            StartedAt = DateTimeOffset.UtcNow,
            FileHash = "completed-hash",
            Status = ProcessingStatus.Completed,
            BatchProcessorName = "test-processor"
        };

        var failedBatch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            BatchType = StagingBatchType.WebScrape,
            SupplierId = supplier.Id,
            StartedAt = DateTimeOffset.UtcNow,
            FileHash = "failed-hash",
            Status = ProcessingStatus.Failed,
            BatchProcessorName = "test-processor"
        };

        Context.StagingBatches.AddRange(queuedBatch, completedBatch, failedBatch);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mockProcessor = Substitute.For<IBatchProcessor>();
        _mockFactory.GetProcessor("test-processor").Returns(mockProcessor);

        // Act
        await ProcessingService.ProcessBatchesAsync(TestContext.Current.CancellationToken);

        // Assert
        // Only the queued batch should be processed
        await mockProcessor.Received(1).ProcessBatchAsync(queuedBatch, TestContext.Current.CancellationToken);
        await mockProcessor.DidNotReceive().ProcessBatchAsync(completedBatch, Arg.Any<CancellationToken>());
        await mockProcessor.DidNotReceive().ProcessBatchAsync(failedBatch, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessBatchesAsync_ProcessesBatchesInStartedAtOrder()
    {
        // Arrange
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = "Test Supplier"
        };
        Context.Suppliers.Add(supplier);

        var olderBatch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            BatchType = StagingBatchType.WebScrape,
            SupplierId = supplier.Id,
            StartedAt = DateTimeOffset.UtcNow.AddHours(-2),
            FileHash = "older-hash",
            Status = ProcessingStatus.Queued,
            BatchProcessorName = "test-processor"
        };

        var newerBatch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            BatchType = StagingBatchType.WebScrape,
            SupplierId = supplier.Id,
            StartedAt = DateTimeOffset.UtcNow.AddHours(-1),
            FileHash = "newer-hash",
            Status = ProcessingStatus.Queued,
            BatchProcessorName = "test-processor"
        };

        // Add in reverse order to test sorting
        Context.StagingBatches.AddRange(newerBatch, olderBatch);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mockProcessor = Substitute.For<IBatchProcessor>();
        _mockFactory.GetProcessor("test-processor").Returns(mockProcessor);

        // Act
        await ProcessingService.ProcessBatchesAsync(TestContext.Current.CancellationToken);

        // Assert
        // Verify both batches were processed, older first
        Received.InOrder(() =>
        {
            mockProcessor.ProcessBatchAsync(olderBatch, TestContext.Current.CancellationToken);
            mockProcessor.ProcessBatchAsync(newerBatch, TestContext.Current.CancellationToken);
        });
    }
}
