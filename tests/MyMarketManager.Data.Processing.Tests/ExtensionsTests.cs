using Microsoft.Extensions.DependencyInjection;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
using MyMarketManager.Data.Processing;
using MyMarketManager.Tests.Shared;

namespace MyMarketManager.Data.Processing.Tests;

/// <summary>
/// Tests for the extension methods in the Extensions class.
/// </summary>
[Trait(TestCategories.Key, TestCategories.Values.Processing)]
public class ExtensionsTests
{
    [Fact]
    public void AddBatchProcessorFactory_RegistersFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddBatchProcessorFactory();

        // Assert
        Assert.NotNull(builder);
        Assert.IsAssignableFrom<IBatchProcessorFactoryBuilder>(builder);

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetService<IBatchProcessorFactory>();
        Assert.NotNull(factory);
    }

    [Fact]
    public void AddBatchProcessorFactory_ReturnsBuilderForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddBatchProcessorFactory();

        // Assert
        Assert.NotNull(builder);
        Assert.Same(services, builder.Services);
    }

    [Fact]
    public void AddWebScraper_RegistersProcessorForWebScrapeType()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddBatchProcessorFactory();

        // Act
        builder.AddWebScraper<TestWebScraperProcessor>("test-web-scraper");

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IBatchProcessorFactory>();

        var processor = factory.GetProcessor("test-web-scraper");
        Assert.NotNull(processor);
        Assert.IsType<TestWebScraperProcessor>(processor);

        var availableProcessors = factory.GetAvailableProcessors(StagingBatchType.WebScrape).ToList();
        Assert.Contains("test-web-scraper", availableProcessors);
    }

    [Fact]
    public void AddWebScraper_DoesNotAppearInBlobUploadProcessors()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddBatchProcessorFactory();

        // Act
        builder.AddWebScraper<TestWebScraperProcessor>("test-web-scraper");

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IBatchProcessorFactory>();

        var blobProcessors = factory.GetAvailableProcessors(StagingBatchType.BlobUpload).ToList();
        Assert.DoesNotContain("test-web-scraper", blobProcessors);
    }

    [Fact]
    public void AddBlobStorageProcessor_RegistersProcessorForBlobUploadType()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddBatchProcessorFactory();

        // Act
        builder.AddBlobStorageProcessor<TestBlobProcessor>("test-blob-processor");

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IBatchProcessorFactory>();

        var processor = factory.GetProcessor("test-blob-processor");
        Assert.NotNull(processor);
        Assert.IsType<TestBlobProcessor>(processor);

        var availableProcessors = factory.GetAvailableProcessors(StagingBatchType.BlobUpload).ToList();
        Assert.Contains("test-blob-processor", availableProcessors);
    }

    [Fact]
    public void AddBlobStorageProcessor_DoesNotAppearInWebScrapeProcessors()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddBatchProcessorFactory();

        // Act
        builder.AddBlobStorageProcessor<TestBlobProcessor>("test-blob-processor");

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IBatchProcessorFactory>();

        var webScrapers = factory.GetAvailableProcessors(StagingBatchType.WebScrape).ToList();
        Assert.DoesNotContain("test-blob-processor", webScrapers);
    }

    [Fact]
    public void ExtensionMethods_CanBeChained()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddBatchProcessorFactory()
            .AddWebScraper<TestWebScraperProcessor>("web-scraper-1")
            .AddWebScraper<AnotherTestWebScraperProcessor>("web-scraper-2")
            .AddBlobStorageProcessor<TestBlobProcessor>("blob-processor-1");

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IBatchProcessorFactory>();

        // Verify all processors were registered
        Assert.NotNull(factory.GetProcessor("web-scraper-1"));
        Assert.NotNull(factory.GetProcessor("web-scraper-2"));
        Assert.NotNull(factory.GetProcessor("blob-processor-1"));

        // Verify they appear in the correct categories
        var webScrapers = factory.GetAvailableProcessors(StagingBatchType.WebScrape).ToList();
        var blobProcessors = factory.GetAvailableProcessors(StagingBatchType.BlobUpload).ToList();

        Assert.Equal(2, webScrapers.Count);
        Assert.Contains("web-scraper-1", webScrapers);
        Assert.Contains("web-scraper-2", webScrapers);

        Assert.Single(blobProcessors);
        Assert.Contains("blob-processor-1", blobProcessors);
    }

    [Fact]
    public void AddWebScraper_ReturnsBuilderForFurtherChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var originalBuilder = services.AddBatchProcessorFactory();

        // Act
        var result = originalBuilder.AddWebScraper<TestWebScraperProcessor>("test-processor");

        // Assert
        Assert.Same(originalBuilder, result);
    }

    [Fact]
    public void AddBlobStorageProcessor_ReturnsBuilderForFurtherChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var originalBuilder = services.AddBatchProcessorFactory();

        // Act
        var result = originalBuilder.AddBlobStorageProcessor<TestBlobProcessor>("test-processor");

        // Assert
        Assert.Same(originalBuilder, result);
    }

    [Fact]
    public void MixedProcessorTypes_RegisterCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBatchProcessorFactory()
            .AddWebScraper<TestWebScraperProcessor>("shein-scraper")
            .AddWebScraper<AnotherTestWebScraperProcessor>("amazon-scraper")
            .AddBlobStorageProcessor<TestBlobProcessor>("csv-uploader")
            .AddBlobStorageProcessor<AnotherTestBlobProcessor>("excel-uploader");

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IBatchProcessorFactory>();

        // Verify web scrapers
        var webScrapers = factory.GetAvailableProcessors(StagingBatchType.WebScrape).ToList();
        Assert.Equal(2, webScrapers.Count);
        Assert.Contains("shein-scraper", webScrapers);
        Assert.Contains("amazon-scraper", webScrapers);

        // Verify blob processors
        var blobProcessors = factory.GetAvailableProcessors(StagingBatchType.BlobUpload).ToList();
        Assert.Equal(2, blobProcessors.Count);
        Assert.Contains("csv-uploader", blobProcessors);
        Assert.Contains("excel-uploader", blobProcessors);

        // Verify processors are actually retrievable
        Assert.IsType<TestWebScraperProcessor>(factory.GetProcessor("shein-scraper"));
        Assert.IsType<AnotherTestWebScraperProcessor>(factory.GetProcessor("amazon-scraper"));
        Assert.IsType<TestBlobProcessor>(factory.GetProcessor("csv-uploader"));
        Assert.IsType<AnotherTestBlobProcessor>(factory.GetProcessor("excel-uploader"));
    }

    // Test helper classes
    private class TestWebScraperProcessor : IBatchProcessor
    {
        public Task ProcessBatchAsync(StagingBatch batch, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private class AnotherTestWebScraperProcessor : IBatchProcessor
    {
        public Task ProcessBatchAsync(StagingBatch batch, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private class TestBlobProcessor : IBatchProcessor
    {
        public Task ProcessBatchAsync(StagingBatch batch, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private class AnotherTestBlobProcessor : IBatchProcessor
    {
        public Task ProcessBatchAsync(StagingBatch batch, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}