using MyMarketManager.Data.Entities;

namespace MyMarketManager.Data.Processing.Tests;

public class StagingBatchWorkItemTests
{
    [Fact]
    public void Constructor_WithValidBatch_SetsProperties()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        var batch = new StagingBatch
        {
            Id = batchId,
            BatchProcessorName = "Test",
            Status = Enums.ProcessingStatus.Queued
        };

        // Act
        var workItem = new StagingBatchWorkItem(batch);

        // Assert
        Assert.Equal(batchId, workItem.Id);
        Assert.Same(batch, workItem.Batch);
    }

    [Fact]
    public void Constructor_WithNullBatch_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new StagingBatchWorkItem(null!));
    }

    [Fact]
    public void Id_MatchesBatchId()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        var batch = new StagingBatch { Id = batchId };
        var workItem = new StagingBatchWorkItem(batch);

        // Act
        var id = workItem.Id;

        // Assert
        Assert.Equal(batchId, id);
    }

    [Fact]
    public void Batch_ReturnsOriginalBatch()
    {
        // Arrange
        var batch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            BatchProcessorName = "TestProcessor"
        };
        var workItem = new StagingBatchWorkItem(batch);

        // Act
        var retrievedBatch = workItem.Batch;

        // Assert
        Assert.Same(batch, retrievedBatch);
        Assert.Equal("TestProcessor", retrievedBatch.BatchProcessorName);
    }

    [Fact]
    public void ImplementsIWorkItem()
    {
        // Arrange
        var batch = new StagingBatch { Id = Guid.NewGuid() };

        // Act
        var workItem = new StagingBatchWorkItem(batch);

        // Assert
        Assert.IsAssignableFrom<IWorkItem>(workItem);
    }
}
