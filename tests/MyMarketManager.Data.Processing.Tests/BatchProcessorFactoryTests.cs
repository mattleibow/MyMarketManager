using Microsoft.Extensions.DependencyInjection;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
using MyMarketManager.Data.Processing;
using MyMarketManager.Tests.Shared;

namespace MyMarketManager.Data.Processing.Tests;

/// <summary>
/// Tests for the BatchProcessorFactory class through the public interface.
/// </summary>
[Trait(TestCategories.Key, TestCategories.Values.Processing)]
public class BatchProcessorFactoryTests
{
    private readonly IBatchProcessorFactory _factory;

    public BatchProcessorFactoryTests()
    {
        var services = new ServiceCollection();
        
        // Configure the factory with test processors
        var builder = services.AddBatchProcessorFactory();
        builder.AddProcessor<TestBatchProcessor>(StagingBatchType.WebScrape, "test-web-processor");
        builder.AddProcessor<AnotherTestBatchProcessor>(StagingBatchType.WebScrape, "another-web-processor");
        builder.AddProcessor<TestBatchProcessor>(StagingBatchType.BlobUpload, "test-blob-processor");
        
        var serviceProvider = services.BuildServiceProvider();
        _factory = serviceProvider.GetRequiredService<IBatchProcessorFactory>();
    }

    [Fact]
    public void GetProcessor_WithValidProcessorName_ReturnsProcessor()
    {
        // Act
        var result = _factory.GetProcessor("test-web-processor");

        // Assert
        Assert.NotNull(result);
        Assert.IsType<TestBatchProcessor>(result);
    }

    [Fact]
    public void GetProcessor_WithInvalidProcessorName_ReturnsNull()
    {
        // Act
        var result = _factory.GetProcessor("non-existent-processor");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetProcessor_WithNullOrEmptyProcessorName_ReturnsNull()
    {
        // Act & Assert
        Assert.Null(_factory.GetProcessor(null!));
        Assert.Null(_factory.GetProcessor(string.Empty));
        Assert.Null(_factory.GetProcessor("   "));
    }

    [Fact]
    public void GetAvailableProcessors_WithMultipleProcessorsForSameBatchType_ReturnsAllMatching()
    {
        // Act
        var result = _factory.GetAvailableProcessors(StagingBatchType.WebScrape).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("test-web-processor", result);
        Assert.Contains("another-web-processor", result);
    }

    [Fact]
    public void GetAvailableProcessors_WithSpecificBatchType_ReturnsOnlyMatching()
    {
        // Act
        var webScrapers = _factory.GetAvailableProcessors(StagingBatchType.WebScrape).ToList();
        var blobProcessors = _factory.GetAvailableProcessors(StagingBatchType.BlobUpload).ToList();

        // Assert
        Assert.Equal(2, webScrapers.Count);
        Assert.Contains("test-web-processor", webScrapers);
        Assert.Contains("another-web-processor", webScrapers);

        Assert.Single(blobProcessors);
        Assert.Contains("test-blob-processor", blobProcessors);
    }

    [Fact]
    public void GetAvailableProcessors_WithUnusedBatchType_ReturnsEmpty()
    {
        // Create a new factory with no processors for a specific type
        var services = new ServiceCollection();
        services.AddBatchProcessorFactory();
        var serviceProvider = services.BuildServiceProvider();
        var emptyFactory = serviceProvider.GetRequiredService<IBatchProcessorFactory>();

        // Act
        var result = emptyFactory.GetAvailableProcessors(StagingBatchType.WebScrape).ToList();

        // Assert
        Assert.Empty(result);
    }

    // Test helper classes
    private class TestBatchProcessor : IBatchProcessor
    {
        public Task ProcessBatchAsync(StagingBatch batch, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private class AnotherTestBatchProcessor : IBatchProcessor
    {
        public Task ProcessBatchAsync(StagingBatch batch, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}