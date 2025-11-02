using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace MyMarketManager.Processing.Tests;

public class WorkItemProcessingEngineTests
{
    private readonly ILogger<WorkItemProcessingService> _logger;

    public WorkItemProcessingEngineTests()
    {
        _logger = Substitute.For<ILogger<WorkItemProcessingService>>();
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(new WorkItemProcessingServiceOptions());
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new WorkItemProcessingService(null!, _logger, options));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();
        var options = Microsoft.Extensions.Options.Options.Create(new WorkItemProcessingServiceOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new WorkItemProcessingService(serviceProvider, null!, options));
    }

    [Fact]
    public void Constructor_WithNoHandlers_LogsCorrectCount()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();
        var options = Microsoft.Extensions.Options.Options.Create(new WorkItemProcessingServiceOptions());

        // Act
        var engine = new WorkItemProcessingService(serviceProvider, _logger, options);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("initialized with 0 handlers")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void Constructor_InitializesOnce()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackgroundProcessing()
            .AddHandler<TestWorkItemHandler, TestWorkItem>("Test", 5, WorkItemHandlerPurpose.Internal);
        
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var engine = serviceProvider.GetRequiredService<WorkItemProcessingService>();

        // Assert - Engine is ready to use immediately after construction
        var handlers = engine.GetHandlers(WorkItemHandlerPurpose.Internal).ToList();
        Assert.Single(handlers);
        Assert.Contains("Test", handlers);
    }

    [Fact]
    public void GetHandlerNamesByPurpose_WithNoHandlers_ReturnsEmpty()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackgroundProcessing(); // Register engine with no handlers
        
        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<WorkItemProcessingService>();

        // Act
        var handlers = engine.GetHandlers(WorkItemHandlerPurpose.Ingestion);

        // Assert
        Assert.Empty(handlers);
    }

    [Fact]
    public void GetHandlerNamesByPurpose_WithMultiplePurposes_ReturnsCorrectHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackgroundProcessing()
            .AddHandler<TestWorkItemHandler, TestWorkItem>("Handler1", 5, WorkItemHandlerPurpose.Ingestion)
            .AddHandler<AnotherTestWorkItemHandler, TestWorkItem>("Handler2", 10, WorkItemHandlerPurpose.Ingestion)
            .AddHandler<ThirdTestWorkItemHandler, TestWorkItem>("Handler3", 15, WorkItemHandlerPurpose.Internal);

        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<WorkItemProcessingService>();

        // Act
        var ingestionHandlers = engine.GetHandlers(WorkItemHandlerPurpose.Ingestion).ToList();
        var internalHandlers = engine.GetHandlers(WorkItemHandlerPurpose.Internal).ToList();
        var exportHandlers = engine.GetHandlers(WorkItemHandlerPurpose.Export).ToList();

        // Assert
        Assert.Equal(2, ingestionHandlers.Count);
        Assert.Contains("Handler1", ingestionHandlers);
        Assert.Contains("Handler2", ingestionHandlers);

        Assert.Single(internalHandlers);
        Assert.Contains("Handler3", internalHandlers);

        Assert.Empty(exportHandlers);
    }

    [Fact]
    public async Task ProcessCycleAsync_WithNoHandlers_CompletesSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackgroundProcessing(); // Register engine with no handlers
        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<WorkItemProcessingService>();

        // Act
        await engine.ProcessCycleAsync(CancellationToken.None);

        // Assert - Should complete without throwing (logs debug message)
    }

    [Fact]
    public async Task ProcessCycleAsync_WithHandlerReturningNoItems_CompletesSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackgroundProcessing()
            .AddHandler<TestWorkItemHandler, TestWorkItem>("Test", 5, WorkItemHandlerPurpose.Internal);

        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<WorkItemProcessingService>();

        // Act
        await engine.ProcessCycleAsync(CancellationToken.None);

        // Assert - Should complete without throwing
    }

    [Fact]
    public async Task ProcessCycleAsync_WithHandlerReturningItems_ProcessesAllItems()
    {
        // Arrange
        var processedItems = new List<Guid>();
        var workItems = new List<TestWorkItem>
        {
            new TestWorkItem(),
            new TestWorkItem(),
            new TestWorkItem()
        };

        TrackingTestWorkItemHandler.Configure(workItems, processedItems);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackgroundProcessing()
            .AddHandler<TrackingTestWorkItemHandler, TestWorkItem>("Test", 10, WorkItemHandlerPurpose.Internal);

        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<WorkItemProcessingService>();

        // Act
        await engine.ProcessCycleAsync(CancellationToken.None);

        // Assert
        Assert.Equal(3, processedItems.Count);
        Assert.Contains(workItems[0].Id, processedItems);
        Assert.Contains(workItems[1].Id, processedItems);
        Assert.Contains(workItems[2].Id, processedItems);
    }

    [Fact]
    public async Task ProcessCycleAsync_WithMultipleHandlers_ProcessesAllItemsFromAllHandlers()
    {
        // Arrange
        var processedItems1 = new List<Guid>();
        var processedItems2 = new List<Guid>();
        
        var workItems1 = new List<TestWorkItem> { new TestWorkItem(), new TestWorkItem() };
        var workItems2 = new List<TestWorkItem> { new TestWorkItem(), new TestWorkItem(), new TestWorkItem() };

        TrackingTestWorkItemHandler.Configure(workItems1, processedItems1);
        AnotherTrackingTestWorkItemHandler.Configure(workItems2, processedItems2);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackgroundProcessing()
            .AddHandler<TrackingTestWorkItemHandler, TestWorkItem>("Handler1", 10, WorkItemHandlerPurpose.Internal)
            .AddHandler<AnotherTrackingTestWorkItemHandler, TestWorkItem>("Handler2", 10, WorkItemHandlerPurpose.Internal);

        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<WorkItemProcessingService>();

        // Act
        await engine.ProcessCycleAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, processedItems1.Count);
        Assert.Equal(3, processedItems2.Count);
    }

    [Fact]
    public async Task ProcessCycleAsync_WithHandlerExceedingMaxItems_TruncatesItems()
    {
        // Arrange
        var processedItems = new List<Guid>();
        var workItems = new List<TestWorkItem>
        {
            new TestWorkItem(),
            new TestWorkItem(),
            new TestWorkItem(),
            new TestWorkItem(),
            new TestWorkItem()
        };

        TrackingTestWorkItemHandler.Configure(workItems, processedItems);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackgroundProcessing()
            .AddHandler<TrackingTestWorkItemHandler, TestWorkItem>("Test", 3, WorkItemHandlerPurpose.Internal); // Max 3 items

        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<WorkItemProcessingService>();

        // Act
        await engine.ProcessCycleAsync(CancellationToken.None);

        // Assert
        Assert.Equal(3, processedItems.Count); // Should only process 3, not 5
    }

    [Fact]
    public async Task ProcessCycleAsync_WithCancellation_StopsProcessing()
    {
        // Arrange
        var processedItems = new List<Guid>();
        var workItems = new List<TestWorkItem> { new TestWorkItem() };
        
        TrackingTestWorkItemHandler.Configure(workItems, processedItems);

        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackgroundProcessing()
            .AddHandler<TrackingTestWorkItemHandler, TestWorkItem>("Test", 10, WorkItemHandlerPurpose.Internal);

        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<WorkItemProcessingService>();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await engine.ProcessCycleAsync(cts.Token));
    }

    [Fact]
    public async Task ProcessCycleAsync_WithHandlerThrowingException_LogsErrorAndContinues()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<FailingTestWorkItemHandler>();
        services.AddBackgroundProcessing()
            .AddHandler<FailingTestWorkItemHandler, TestWorkItem>("Test", 10, WorkItemHandlerPurpose.Internal);

        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<WorkItemProcessingService>();

        // Act & Assert - Should not throw, error is logged
        await engine.ProcessCycleAsync(CancellationToken.None);
    }

    // Test helper classes
    private class TestWorkItem : IWorkItem
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    private class TestWorkItemHandler : IWorkItemHandler<TestWorkItem>
    {
        public Task<IReadOnlyCollection<TestWorkItem>> FetchWorkItemsAsync(int maxItems, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<TestWorkItem>>(Array.Empty<TestWorkItem>());
        }

        public Task ProcessAsync(TestWorkItem workItem, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private class AnotherTestWorkItemHandler : IWorkItemHandler<TestWorkItem>
    {
        public Task<IReadOnlyCollection<TestWorkItem>> FetchWorkItemsAsync(int maxItems, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<TestWorkItem>>(Array.Empty<TestWorkItem>());
        }

        public Task ProcessAsync(TestWorkItem workItem, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private class ThirdTestWorkItemHandler : IWorkItemHandler<TestWorkItem>
    {
        public Task<IReadOnlyCollection<TestWorkItem>> FetchWorkItemsAsync(int maxItems, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<TestWorkItem>>(Array.Empty<TestWorkItem>());
        }

        public Task ProcessAsync(TestWorkItem workItem, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private class TrackingTestWorkItemHandler : IWorkItemHandler<TestWorkItem>
    {
        private static List<TestWorkItem>? _itemsToReturn;
        private static List<Guid>? _processedItems;

        public static void Configure(List<TestWorkItem> itemsToReturn, List<Guid> processedItems)
        {
            _itemsToReturn = itemsToReturn;
            _processedItems = processedItems;
        }

        public Task<IReadOnlyCollection<TestWorkItem>> FetchWorkItemsAsync(int maxItems, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<TestWorkItem>>(_itemsToReturn ?? new List<TestWorkItem>());
        }

        public Task ProcessAsync(TestWorkItem workItem, CancellationToken cancellationToken)
        {
            _processedItems?.Add(workItem.Id);
            return Task.CompletedTask;
        }
    }

    private class AnotherTrackingTestWorkItemHandler : IWorkItemHandler<TestWorkItem>
    {
        private static List<TestWorkItem>? _itemsToReturn;
        private static List<Guid>? _processedItems;

        public static void Configure(List<TestWorkItem> itemsToReturn, List<Guid> processedItems)
        {
            _itemsToReturn = itemsToReturn;
            _processedItems = processedItems;
        }

        public Task<IReadOnlyCollection<TestWorkItem>> FetchWorkItemsAsync(int maxItems, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<TestWorkItem>>(_itemsToReturn ?? new List<TestWorkItem>());
        }

        public Task ProcessAsync(TestWorkItem workItem, CancellationToken cancellationToken)
        {
            _processedItems?.Add(workItem.Id);
            return Task.CompletedTask;
        }
    }

    private class FailingTestWorkItemHandler : IWorkItemHandler<TestWorkItem>
    {
        public Task<IReadOnlyCollection<TestWorkItem>> FetchWorkItemsAsync(int maxItems, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<TestWorkItem>>(new[] { new TestWorkItem() });
        }

        public Task ProcessAsync(TestWorkItem workItem, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Simulated processing failure");
        }
    }
}
