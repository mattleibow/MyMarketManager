using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
using MyMarketManager.Data.Processing;
using MyMarketManager.Tests.Shared;
using MyMarketManager.WebApp.Services;

namespace MyMarketManager.Data.Processing.Tests;

/// <summary>
/// Integration tests for the complete batch processing workflow.
/// Uses real database context with SQLite to test end-to-end scenarios.
/// </summary>
[Trait(TestCategories.Key, TestCategories.Values.Processing)]
[Trait(TestCategories.Key, TestCategories.Values.Database)]
public class BatchProcessingIntegrationTests(ITestOutputHelper outputHelper) : SqliteTestBase(outputHelper)
{
    private IServiceProvider? _serviceProvider;
    private IBatchProcessorFactory? _factory;
    private ILogger<BatchProcessingService>? _logger;

    private BatchProcessingService ProcessingService { get; set; } = null!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        // Set up the DI container with the initialized Context
        var services = new ServiceCollection();
        
        services.AddLogging();
        services.AddSingleton(Context);
        
        // Configure batch processing with test processors
        services.AddBatchProcessorFactory()
            .AddWebScraper<TestOrderProcessor>("order-processor")
            .AddBlobStorageProcessor<TestSalesProcessor>("sales-processor");

        _serviceProvider = services.BuildServiceProvider();
        _factory = _serviceProvider.GetRequiredService<IBatchProcessorFactory>();
        _logger = _serviceProvider.GetRequiredService<ILogger<BatchProcessingService>>();

        ProcessingService = new BatchProcessingService(Context, _factory!, _logger!);
    }

    [Fact]
    public async Task EndToEndProcessing_WithWebScrapeType_ProcessesSuccessfully()
    {
        // Arrange
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = "Test Supplier",
            WebsiteUrl = "https://test-supplier.com"
        };
        Context.Suppliers.Add(supplier);

        var batch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            BatchType = StagingBatchType.WebScrape,
            SupplierId = supplier.Id,
            StartedAt = DateTimeOffset.UtcNow,
            FileHash = "integration-test-hash",
            Status = ProcessingStatus.Queued,
            BatchProcessorName = "order-processor",
            Notes = "Integration test batch"
        };
        Context.StagingBatches.Add(batch);

        // Add some staging orders to the batch
        var stagingOrder = new StagingPurchaseOrder
        {
            Id = Guid.NewGuid(),
            StagingBatchId = batch.Id,
            SupplierReference = "TEST-ORDER-001",
            OrderDate = DateTimeOffset.UtcNow,
            RawData = "{\"orderId\":\"TEST-ORDER-001\",\"items\":[{\"name\":\"Test Product\",\"quantity\":5,\"price\":19.99}]}",
            Status = ProcessingStatus.Queued
        };
        Context.StagingPurchaseOrders.Add(stagingOrder);

        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await ProcessingService.ProcessBatchesAsync(TestContext.Current.CancellationToken);

        // Assert
        var processedBatch = await Context.StagingBatches.FindAsync(batch.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(processedBatch);
        Assert.Equal(ProcessingStatus.Completed, processedBatch.Status);
        Assert.NotNull(processedBatch.CompletedAt);
        Assert.Null(processedBatch.ErrorMessage);
    }

    [Fact]
    public async Task EndToEndProcessing_WithBlobUploadType_ProcessesSuccessfully()
    {
        // Arrange
        var batch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            BatchType = StagingBatchType.BlobUpload,
            StartedAt = DateTimeOffset.UtcNow,
            FileHash = "sales-integration-test-hash",
            Status = ProcessingStatus.Queued,
            BatchProcessorName = "sales-processor",
            Notes = "Sales data integration test",
            FileContents = "Date,Product,Quantity,Amount\n2024-01-01,Test Product,2,39.98\n2024-01-02,Another Product,1,25.00"
        };
        Context.StagingBatches.Add(batch);

        // Add some staging sales to the batch
        var stagingSale = new StagingSale
        {
            Id = Guid.NewGuid(),
            StagingBatchId = batch.Id,
            SaleDate = DateTimeOffset.UtcNow,
            RawData = "{\"date\":\"2024-01-01\",\"product\":\"Test Product\",\"quantity\":2,\"amount\":39.98}"
        };
        Context.StagingSales.Add(stagingSale);

        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await ProcessingService.ProcessBatchesAsync(TestContext.Current.CancellationToken);

        // Assert
        var processedBatch = await Context.StagingBatches.FindAsync(batch.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(processedBatch);
        Assert.Equal(ProcessingStatus.Completed, processedBatch.Status);
        Assert.NotNull(processedBatch.CompletedAt);
        Assert.Null(processedBatch.ErrorMessage);
    }

    [Fact]
    public async Task EndToEndProcessing_WithMultipleBatches_ProcessesInOrder()
    {
        // Arrange
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = "Multi-Batch Supplier"
        };
        Context.Suppliers.Add(supplier);

        var olderBatch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            BatchType = StagingBatchType.WebScrape,
            SupplierId = supplier.Id,
            StartedAt = DateTimeOffset.UtcNow.AddHours(-2),
            FileHash = "older-batch-hash",
            Status = ProcessingStatus.Queued,
            BatchProcessorName = "order-processor"
        };

        var newerBatch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            BatchType = StagingBatchType.BlobUpload,
            StartedAt = DateTimeOffset.UtcNow.AddHours(-1),
            FileHash = "newer-batch-hash",
            Status = ProcessingStatus.Queued,
            BatchProcessorName = "sales-processor"
        };

        Context.StagingBatches.AddRange(newerBatch, olderBatch); // Add in reverse order
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await ProcessingService.ProcessBatchesAsync(TestContext.Current.CancellationToken);

        // Assert
        var processedOlderBatch = await Context.StagingBatches.FindAsync(olderBatch.Id, TestContext.Current.CancellationToken);
        var processedNewerBatch = await Context.StagingBatches.FindAsync(newerBatch.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(processedOlderBatch);
        Assert.NotNull(processedNewerBatch);
        Assert.Equal(ProcessingStatus.Completed, processedOlderBatch.Status);
        Assert.Equal(ProcessingStatus.Completed, processedNewerBatch.Status);

        // Older batch should have been processed first (earlier CompletedAt)
        Assert.True(processedOlderBatch.CompletedAt <= processedNewerBatch.CompletedAt);
    }

    [Fact]
    public async Task EndToEndProcessing_WithProcessorException_MarksBatchAsFailed()
    {
        // Arrange
        var batch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            BatchType = StagingBatchType.WebScrape,
            StartedAt = DateTimeOffset.UtcNow,
            FileHash = "failing-batch-hash",
            Status = ProcessingStatus.Queued,
            BatchProcessorName = "failing-processor" // This processor will throw
        };
        Context.StagingBatches.Add(batch);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Register a failing processor
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Context);
        services.AddBatchProcessorFactory()
            .AddWebScraper<FailingTestProcessor>("failing-processor");

        var failingServiceProvider = services.BuildServiceProvider();
        var failingFactory = failingServiceProvider.GetRequiredService<IBatchProcessorFactory>();
        var failingLogger = failingServiceProvider.GetRequiredService<ILogger<BatchProcessingService>>();
        var failingService = new BatchProcessingService(Context, failingFactory, failingLogger);

        // Act
        await failingService.ProcessBatchesAsync(TestContext.Current.CancellationToken);

        // Assert
        var processedBatch = await Context.StagingBatches.FindAsync(batch.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(processedBatch);
        Assert.Equal(ProcessingStatus.Failed, processedBatch.Status);
        Assert.NotNull(processedBatch.ErrorMessage);
        Assert.Contains("Integration test failure", processedBatch.ErrorMessage);
        Assert.Null(processedBatch.CompletedAt);
    }

    [Fact]
    public async Task FactoryRegistration_WithRealDIContainer_WorksCorrectly()
    {
        // This test verifies that the factory and processors work correctly with a real DI container
        
        // Assert
        Assert.NotNull(_factory);
        
        // Test web scraper registration
        var orderProcessor = _factory.GetProcessor("order-processor");
        Assert.NotNull(orderProcessor);
        Assert.IsType<TestOrderProcessor>(orderProcessor);

        // Test blob processor registration
        var salesProcessor = _factory.GetProcessor("sales-processor");
        Assert.NotNull(salesProcessor);
        Assert.IsType<TestSalesProcessor>(salesProcessor);

        // Test category filtering
        var webScrapers = _factory.GetAvailableProcessors(StagingBatchType.WebScrape).ToList();
        var blobProcessors = _factory.GetAvailableProcessors(StagingBatchType.BlobUpload).ToList();

        Assert.Contains("order-processor", webScrapers);
        Assert.DoesNotContain("sales-processor", webScrapers);

        Assert.Contains("sales-processor", blobProcessors);
        Assert.DoesNotContain("order-processor", blobProcessors);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_serviceProvider is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        await base.DisposeAsync();
    }

    // Test processor implementations
    private class TestOrderProcessor : IBatchProcessor
    {
        public Task ProcessBatchAsync(StagingBatch batch, CancellationToken cancellationToken)
        {
            // Simulate order processing work
            batch.Notes = $"Processed by TestOrderProcessor at {DateTimeOffset.UtcNow}";
            return Task.CompletedTask;
        }
    }

    private class TestSalesProcessor : IBatchProcessor
    {
        public Task ProcessBatchAsync(StagingBatch batch, CancellationToken cancellationToken)
        {
            // Simulate sales processing work
            batch.Notes = $"Processed by TestSalesProcessor at {DateTimeOffset.UtcNow}";
            return Task.CompletedTask;
        }
    }

    private class FailingTestProcessor : IBatchProcessor
    {
        public Task ProcessBatchAsync(StagingBatch batch, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Integration test failure - processor intentionally failed");
        }
    }
}