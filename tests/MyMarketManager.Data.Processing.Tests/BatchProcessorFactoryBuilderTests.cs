using Microsoft.Extensions.DependencyInjection;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
using MyMarketManager.Data.Processing;
using MyMarketManager.Tests.Shared;

namespace MyMarketManager.Data.Processing.Tests;

/// <summary>
/// Tests for the BatchProcessorFactoryBuilder class.
/// </summary>
[Trait(TestCategories.Key, TestCategories.Values.Processing)]
public class BatchProcessorFactoryBuilderTests
{
    [Fact]
    public void AddProcessor_RegistersProcessorCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddBatchProcessorFactory();

        // Act
        builder.AddProcessor<TestBatchProcessor>(StagingBatchType.WebScrape, "test-processor");

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IBatchProcessorFactory>();
        
        var processor = factory.GetProcessor("test-processor");
        Assert.NotNull(processor);
        Assert.IsType<TestBatchProcessor>(processor);
    }

    [Fact]
    public void AddProcessor_WithMultipleProcessors_RegistersAllCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddBatchProcessorFactory();

        // Act
        builder.AddProcessor<TestBatchProcessor>(StagingBatchType.WebScrape, "processor1")
               .AddProcessor<AnotherTestBatchProcessor>(StagingBatchType.BlobUpload, "processor2")
               .AddProcessor<ThirdTestBatchProcessor>(StagingBatchType.WebScrape, "processor3");

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IBatchProcessorFactory>();

        var processor1 = factory.GetProcessor("processor1");
        var processor2 = factory.GetProcessor("processor2");
        var processor3 = factory.GetProcessor("processor3");

        Assert.IsType<TestBatchProcessor>(processor1);
        Assert.IsType<AnotherTestBatchProcessor>(processor2);
        Assert.IsType<ThirdTestBatchProcessor>(processor3);
    }

    [Fact]
    public void AddProcessor_WithSameBatchType_AllowsMultipleProcessors()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddBatchProcessorFactory();

        // Act
        builder.AddProcessor<TestBatchProcessor>(StagingBatchType.WebScrape, "web-processor-1")
               .AddProcessor<AnotherTestBatchProcessor>(StagingBatchType.WebScrape, "web-processor-2");

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IBatchProcessorFactory>();

        var availableProcessors = factory.GetAvailableProcessors(StagingBatchType.WebScrape).ToList();
        Assert.Equal(2, availableProcessors.Count);
        Assert.Contains("web-processor-1", availableProcessors);
        Assert.Contains("web-processor-2", availableProcessors);
    }

    [Fact]
    public void AddProcessor_WithDuplicateProcessorName_OverwritesPreviousRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddBatchProcessorFactory();

        // Act - Register same processor name twice with different types
        builder.AddProcessor<TestBatchProcessor>(StagingBatchType.WebScrape, "duplicate-name")
               .AddProcessor<AnotherTestBatchProcessor>(StagingBatchType.BlobUpload, "duplicate-name");

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IBatchProcessorFactory>();

        var processor = factory.GetProcessor("duplicate-name");
        Assert.NotNull(processor);
        // Should be the second registration (AnotherTestBatchProcessor)
        Assert.IsType<AnotherTestBatchProcessor>(processor);

        // Should only appear in BlobUpload processors, not WebScrape
        var webScrapers = factory.GetAvailableProcessors(StagingBatchType.WebScrape).ToList();
        var blobProcessors = factory.GetAvailableProcessors(StagingBatchType.BlobUpload).ToList();

        Assert.DoesNotContain("duplicate-name", webScrapers);
        Assert.Contains("duplicate-name", blobProcessors);
    }

    [Fact]
    public void AddProcessor_ReturnsBuilderForChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddBatchProcessorFactory();

        // Act & Assert - Should be able to chain calls
        var result = builder.AddProcessor<TestBatchProcessor>(StagingBatchType.WebScrape, "processor1")
                           .AddProcessor<AnotherTestBatchProcessor>(StagingBatchType.BlobUpload, "processor2");

        Assert.Same(builder, result);
    }

    [Fact]
    public void AddProcessor_RegistersProcessorAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddBatchProcessorFactory();
        builder.AddProcessor<TestBatchProcessor>(StagingBatchType.WebScrape, "test-processor");

        // Act
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Should get same instance within scope, different across scopes
        using var scope1 = serviceProvider.CreateScope();
        using var scope2 = serviceProvider.CreateScope();

        var factory1 = scope1.ServiceProvider.GetRequiredService<IBatchProcessorFactory>();
        var factory2 = scope2.ServiceProvider.GetRequiredService<IBatchProcessorFactory>();

        var processor1a = factory1.GetProcessor("test-processor");
        var processor1b = factory1.GetProcessor("test-processor");
        var processor2 = factory2.GetProcessor("test-processor");

        // Same within scope
        Assert.Same(processor1a, processor1b);
        // Different across scopes
        Assert.NotSame(processor1a, processor2);
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

    private class ThirdTestBatchProcessor : IBatchProcessor
    {
        public Task ProcessBatchAsync(StagingBatch batch, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}