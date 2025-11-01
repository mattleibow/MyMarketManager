using Microsoft.Extensions.DependencyInjection;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
using MyMarketManager.Data.Processing;
using MyMarketManager.Tests.Shared;

namespace MyMarketManager.Data.Processing.Tests;

/// <summary>
/// Tests for processor purpose and metadata functionality.
/// </summary>
[Trait(TestCategories.Key, TestCategories.Values.Processing)]
public class ProcessorPurposeTests
{
    [Fact]
    public void GetProcessorsByPurpose_ReturnsOnlyMatchingPurpose()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBatchProcessorFactory()
            .AddWebScraper<TestIngestionProcessor>("ingestion-processor", ProcessorPurpose.Ingestion)
            .AddWebScraper<TestInternalProcessor>("internal-processor", ProcessorPurpose.Internal);

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IBatchProcessorFactory>();

        // Act
        var ingestionProcessors = factory.GetProcessorsByPurpose(ProcessorPurpose.Ingestion).ToList();
        var internalProcessors = factory.GetProcessorsByPurpose(ProcessorPurpose.Internal).ToList();

        // Assert
        Assert.Single(ingestionProcessors);
        Assert.Contains("ingestion-processor", ingestionProcessors);
        Assert.Single(internalProcessors);
        Assert.Contains("internal-processor", internalProcessors);
    }

    [Fact]
    public void GetAvailableProcessors_WithBatchTypeAndPurpose_ReturnsOnlyMatching()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBatchProcessorFactory()
            .AddWebScraper<TestIngestionProcessor>("web-ingestion", ProcessorPurpose.Ingestion)
            .AddWebScraper<TestInternalProcessor>("web-internal", ProcessorPurpose.Internal)
            .AddBlobStorageProcessor<TestIngestionProcessor>("blob-ingestion", ProcessorPurpose.Ingestion);

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IBatchProcessorFactory>();

        // Act
        var webIngestion = factory.GetAvailableProcessors(StagingBatchType.WebScrape, ProcessorPurpose.Ingestion).ToList();
        var webInternal = factory.GetAvailableProcessors(StagingBatchType.WebScrape, ProcessorPurpose.Internal).ToList();
        var blobIngestion = factory.GetAvailableProcessors(StagingBatchType.BlobUpload, ProcessorPurpose.Ingestion).ToList();

        // Assert
        Assert.Single(webIngestion);
        Assert.Contains("web-ingestion", webIngestion);
        
        Assert.Single(webInternal);
        Assert.Contains("web-internal", webInternal);
        
        Assert.Single(blobIngestion);
        Assert.Contains("blob-ingestion", blobIngestion);
    }

    [Fact]
    public void AddProcessor_WithoutPurpose_DefaultsToIngestion()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBatchProcessorFactory()
            .AddWebScraper<TestIngestionProcessor>("default-processor");

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IBatchProcessorFactory>();

        // Act
        var metadata = factory.GetProcessorMetadata("default-processor");

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(ProcessorPurpose.Ingestion, metadata.Purpose);
    }

    [Fact]
    public void GetProcessorMetadata_ReturnsCorrectMetadata()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBatchProcessorFactory()
            .AddWebScraper<TestIngestionProcessor>(
                "test-processor",
                ProcessorPurpose.Ingestion,
                "Test Processor",
                "A test processor for unit tests");

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IBatchProcessorFactory>();

        // Act
        var metadata = factory.GetProcessorMetadata("test-processor");

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(StagingBatchType.WebScrape, metadata.BatchType);
        Assert.Equal(typeof(TestIngestionProcessor), metadata.ProcessorType);
        Assert.Equal(ProcessorPurpose.Ingestion, metadata.Purpose);
        Assert.Equal("Test Processor", metadata.DisplayName);
        Assert.Equal("A test processor for unit tests", metadata.Description);
    }

    [Fact]
    public void GetProcessorMetadata_WithInvalidName_ReturnsNull()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBatchProcessorFactory();

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IBatchProcessorFactory>();

        // Act
        var metadata = factory.GetProcessorMetadata("non-existent");

        // Assert
        Assert.Null(metadata);
    }

    [Fact]
    public void GetProcessorsByPurpose_WithNoMatchingProcessors_ReturnsEmpty()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBatchProcessorFactory()
            .AddWebScraper<TestIngestionProcessor>("ingestion-only", ProcessorPurpose.Ingestion);

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IBatchProcessorFactory>();

        // Act
        var exportProcessors = factory.GetProcessorsByPurpose(ProcessorPurpose.Export).ToList();

        // Assert
        Assert.Empty(exportProcessors);
    }

    [Fact]
    public void MultipleProcessors_WithMixedPurposes_CanBeRetrievedIndependently()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBatchProcessorFactory()
            .AddWebScraper<TestIngestionProcessor>("shein-scraper", ProcessorPurpose.Ingestion, "Shein Web Scraper")
            .AddWebScraper<TestInternalProcessor>("vectorization", ProcessorPurpose.Internal, "Image Vectorization")
            .AddBlobStorageProcessor<TestIngestionProcessor>("csv-import", ProcessorPurpose.Ingestion, "CSV Import")
            .AddBlobStorageProcessor<TestInternalProcessor>("cleanup", ProcessorPurpose.Internal, "Data Cleanup");

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IBatchProcessorFactory>();

        // Act
        var allIngestion = factory.GetProcessorsByPurpose(ProcessorPurpose.Ingestion).ToList();
        var allInternal = factory.GetProcessorsByPurpose(ProcessorPurpose.Internal).ToList();
        var webScrapers = factory.GetAvailableProcessors(StagingBatchType.WebScrape).ToList();

        // Assert - Ingestion processors
        Assert.Equal(2, allIngestion.Count);
        Assert.Contains("shein-scraper", allIngestion);
        Assert.Contains("csv-import", allIngestion);

        // Assert - Internal processors
        Assert.Equal(2, allInternal.Count);
        Assert.Contains("vectorization", allInternal);
        Assert.Contains("cleanup", allInternal);

        // Assert - Web scrapers (regardless of purpose)
        Assert.Equal(2, webScrapers.Count);
        Assert.Contains("shein-scraper", webScrapers);
        Assert.Contains("vectorization", webScrapers);

        // Verify metadata
        var sheinMetadata = factory.GetProcessorMetadata("shein-scraper");
        Assert.NotNull(sheinMetadata);
        Assert.Equal("Shein Web Scraper", sheinMetadata.DisplayName);
        Assert.Equal(ProcessorPurpose.Ingestion, sheinMetadata.Purpose);
    }

    // Test helper classes
    private class TestIngestionProcessor : IBatchProcessor
    {
        public Task ProcessBatchAsync(StagingBatch batch, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private class TestInternalProcessor : IBatchProcessor
    {
        public Task ProcessBatchAsync(StagingBatch batch, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
