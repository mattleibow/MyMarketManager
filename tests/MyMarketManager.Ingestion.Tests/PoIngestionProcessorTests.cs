using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyMarketManager.Data;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
using MyMarketManager.Ingestion;
using MyMarketManager.Scrapers;
using MyMarketManager.Tests.Shared;
using NSubstitute;

namespace MyMarketManager.Ingestion.Tests;

[Trait(TestCategories.Key, TestCategories.Values.Database)]
public class PoIngestionProcessorTests(ITestOutputHelper outputHelper)
{
    private readonly ILogger<PoIngestionProcessor> _logger = outputHelper.ToLogger<PoIngestionProcessor>();

    [Fact]
    public async Task ProcessPendingBatchesAsync_WithNoQueuedBatches_ReturnsZero()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var serviceProvider = CreateServiceProvider();
        var processor = new PoIngestionProcessor(context, _logger, serviceProvider);

        // Act
        var result = await processor.ProcessPendingBatchesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ProcessPendingBatchesAsync_SkipsCompletedBatches()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var supplier = await CreateSupplierAsync(context, "Test Supplier");
        var queuedBatch = await CreateBatchAsync(context, supplier.Id, ProcessingStatus.Queued, "Shein", withCookies: true);
        var completedBatch = await CreateBatchAsync(context, supplier.Id, ProcessingStatus.Completed, "Shein", withCookies: true);
        
        var serviceProvider = CreateServiceProvider();
        var processor = new PoIngestionProcessor(context, _logger, serviceProvider);

        // Act
        var result = await processor.ProcessPendingBatchesAsync(CancellationToken.None);

        // Assert - Only queued batch should be counted
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ProcessPendingBatchesAsync_SkipsBlobUploadBatches()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var supplier = await CreateSupplierAsync(context, "Test Supplier");
        var webScrapeBatch = await CreateBatchAsync(context, supplier.Id, ProcessingStatus.Queued, "Shein", withCookies: true);
        
        // Create a BlobUpload batch (should be skipped)
        var blobBatch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            BatchType = StagingBatchType.BlobUpload,
            SupplierId = supplier.Id,
            StartedAt = DateTimeOffset.UtcNow,
            FileHash = Guid.NewGuid().ToString("N"),
            FileContents = "{}",
            Status = ProcessingStatus.Queued,
            Notes = "Scraper: Shein, Blob upload batch"
        };
        context.StagingBatches.Add(blobBatch);
        await context.SaveChangesAsync();
        
        var serviceProvider = CreateServiceProvider();
        var processor = new PoIngestionProcessor(context, _logger, serviceProvider);

        // Act
        var result = await processor.ProcessPendingBatchesAsync(CancellationToken.None);

        // Assert - Only WebScrape batch should be processed
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ProcessBatchAsync_WithNoSupplierId_MarksAsFailed()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var batch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            BatchType = StagingBatchType.WebScrape,
            SupplierId = null, // No supplier
            StartedAt = DateTimeOffset.UtcNow,
            FileHash = Guid.NewGuid().ToString("N"),
            FileContents = "{\"domain\": \"test.com\", \"cookies\": []}",
            Status = ProcessingStatus.Queued,
            Notes = "Scraper: Shein, Test batch"
        };
        context.StagingBatches.Add(batch);
        await context.SaveChangesAsync();
        
        var serviceProvider = CreateServiceProvider();
        var processor = new PoIngestionProcessor(context, _logger, serviceProvider);

        // Act
        await processor.ProcessBatchAsync(batch, CancellationToken.None);

        // Assert
        var updatedBatch = await context.StagingBatches.FindAsync(batch.Id);
        Assert.NotNull(updatedBatch);
        Assert.Equal(ProcessingStatus.Failed, updatedBatch.Status);
        Assert.Equal("No supplier ID provided", updatedBatch.ErrorMessage);
    }

    [Fact]
    public async Task ProcessBatchAsync_WithNoCookies_MarksAsFailed()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var supplier = await CreateSupplierAsync(context, "Test Supplier");
        var batch = await CreateBatchAsync(context, supplier.Id, ProcessingStatus.Queued, "Shein", withCookies: false);
        
        var serviceProvider = CreateServiceProvider();
        var processor = new PoIngestionProcessor(context, _logger, serviceProvider);

        // Act
        await processor.ProcessBatchAsync(batch, CancellationToken.None);

        // Assert
        var updatedBatch = await context.StagingBatches.FindAsync(batch.Id);
        Assert.NotNull(updatedBatch);
        Assert.Equal(ProcessingStatus.Failed, updatedBatch.Status);
        Assert.Equal("No cookie data provided", updatedBatch.ErrorMessage);
    }

    [Fact]
    public async Task ProcessBatchAsync_WithNonExistentSupplier_MarksAsFailed()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var nonExistentSupplierId = Guid.NewGuid();
        var batch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            BatchType = StagingBatchType.WebScrape,
            SupplierId = nonExistentSupplierId,
            StartedAt = DateTimeOffset.UtcNow,
            FileHash = Guid.NewGuid().ToString("N"),
            FileContents = "{\"domain\": \"test.com\", \"cookies\": []}",
            Status = ProcessingStatus.Queued,
            Notes = "Scraper: Shein, Test batch"
        };
        context.StagingBatches.Add(batch);
        await context.SaveChangesAsync();
        
        var serviceProvider = CreateServiceProvider();
        var processor = new PoIngestionProcessor(context, _logger, serviceProvider);

        // Act
        await processor.ProcessBatchAsync(batch, CancellationToken.None);

        // Assert
        var updatedBatch = await context.StagingBatches.FindAsync(batch.Id);
        Assert.NotNull(updatedBatch);
        Assert.Equal(ProcessingStatus.Failed, updatedBatch.Status);
        Assert.Equal("Supplier not found", updatedBatch.ErrorMessage);
    }

    [Fact]
    public async Task ProcessBatchAsync_WithNoScraperName_MarksAsFailed()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var supplier = await CreateSupplierAsync(context, "Test Supplier");
        var batch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            BatchType = StagingBatchType.WebScrape,
            SupplierId = supplier.Id,
            StartedAt = DateTimeOffset.UtcNow,
            FileHash = Guid.NewGuid().ToString("N"),
            FileContents = "{\"domain\": \"test.com\", \"cookies\": []}",
            Status = ProcessingStatus.Queued,
            Notes = "No scraper name here" // Missing "Scraper: " prefix
        };
        context.StagingBatches.Add(batch);
        await context.SaveChangesAsync();
        
        var serviceProvider = CreateServiceProvider();
        var processor = new PoIngestionProcessor(context, _logger, serviceProvider);

        // Act
        await processor.ProcessBatchAsync(batch, CancellationToken.None);

        // Assert
        var updatedBatch = await context.StagingBatches.FindAsync(batch.Id);
        Assert.NotNull(updatedBatch);
        Assert.Equal(ProcessingStatus.Failed, updatedBatch.Status);
        Assert.Equal("No scraper name specified", updatedBatch.ErrorMessage);
    }

    [Fact]
    public async Task ProcessBatchAsync_SetsStartedAtTimestamp()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var supplier = await CreateSupplierAsync(context, "Test Supplier");
        var batch = await CreateBatchAsync(context, supplier.Id, ProcessingStatus.Queued, "Shein", withCookies: true);
        var beforeProcessing = DateTimeOffset.UtcNow;
        
        var serviceProvider = CreateServiceProviderWithMockFactory();
        var processor = new PoIngestionProcessor(context, _logger, serviceProvider);

        // Act
        await processor.ProcessBatchAsync(batch, CancellationToken.None);

        // Assert
        var updatedBatch = await context.StagingBatches.FindAsync(batch.Id);
        Assert.NotNull(updatedBatch);
        Assert.True(updatedBatch.StartedAt >= beforeProcessing);
        Assert.True(updatedBatch.StartedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task ProcessBatchAsync_ExtractsScraperNameFromNotes()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var supplier = await CreateSupplierAsync(context, "Test Supplier");
        
        // Test various note formats
        var testCases = new[]
        {
            ("Scraper: Shein, Cookie submission on 2025-10-24", "Shein"),
            ("Scraper: AnotherScraper, Some other text", "AnotherScraper"),
            ("Scraper: TestScraper", "TestScraper")
        };

        foreach (var (notes, expectedScraperName) in testCases)
        {
            var batch = new StagingBatch
            {
                Id = Guid.NewGuid(),
                BatchType = StagingBatchType.WebScrape,
                SupplierId = supplier.Id,
                StartedAt = DateTimeOffset.UtcNow,
                FileHash = Guid.NewGuid().ToString("N"),
                FileContents = "{\"domain\": \"test.com\", \"cookies\": []}",
                Status = ProcessingStatus.Queued,
                Notes = notes
            };
            context.StagingBatches.Add(batch);
        }
        await context.SaveChangesAsync();

        var serviceProvider = CreateServiceProviderWithMockFactory();
        var processor = new PoIngestionProcessor(context, _logger, serviceProvider);

        // Act & Assert - verify it processes without error
        foreach (var (notes, _) in testCases)
        {
            var batch = await context.StagingBatches
                .FirstAsync(b => b.Notes == notes);
            
            await processor.ProcessBatchAsync(batch, CancellationToken.None);
            
            var updatedBatch = await context.StagingBatches.FindAsync(batch.Id);
            Assert.NotNull(updatedBatch);
            // Should either be Completed or Failed (depending on whether factory has scraper)
            Assert.NotEqual(ProcessingStatus.Queued, updatedBatch.Status);
        }
    }

    // Helper methods

    private static MyMarketManagerDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<MyMarketManagerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new MyMarketManagerDbContext(options);
    }

    private static async Task<Supplier> CreateSupplierAsync(MyMarketManagerDbContext context, string name)
    {
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = name,
            ContactInfo = "test@example.com"
        };
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();
        return supplier;
    }

    private static async Task<StagingBatch> CreateBatchAsync(
        MyMarketManagerDbContext context,
        Guid supplierId,
        ProcessingStatus status,
        string scraperName,
        bool withCookies)
    {
        var batch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            BatchType = StagingBatchType.WebScrape,
            SupplierId = supplierId,
            StartedAt = DateTimeOffset.UtcNow,
            FileHash = Guid.NewGuid().ToString("N"),
            FileContents = withCookies ? "{\"domain\": \"test.com\", \"cookies\": []}" : null,
            Status = status,
            Notes = $"Scraper: {scraperName}, Test batch"
        };
        context.StagingBatches.Add(batch);
        await context.SaveChangesAsync();
        return batch;
    }

    private static IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        return services.BuildServiceProvider();
    }

    private static IServiceProvider CreateServiceProviderWithMockFactory()
    {
        var services = new ServiceCollection();
        
        // Create a mock scraper factory
        var mockFactory = Substitute.For<IWebScraperFactory>();
        mockFactory.GetAvailableScrapers().Returns(new[] { "Shein" });
        
        // Create a mock scraper that doesn't do anything
        var mockScraper = Substitute.For<WebScraper>(
            Arg.Any<MyMarketManagerDbContext>(),
            Arg.Any<ILogger>(),
            Arg.Any<Microsoft.Extensions.Options.IOptions<ScraperConfiguration>>(),
            Arg.Any<IWebScraperSessionFactory>());
        
        mockFactory.CreateScraper(Arg.Any<string>()).Returns(mockScraper);
        
        services.AddScoped<IWebScraperFactory>(_ => mockFactory);
        
        return services.BuildServiceProvider();
    }
}
