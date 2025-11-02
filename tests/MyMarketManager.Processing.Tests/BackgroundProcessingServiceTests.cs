using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMarketManager.Processing;
using NSubstitute;

namespace MyMarketManager.Processing.Tests;

public class BackgroundProcessingServiceTests
{
    [Fact]
    public void Constructor_WithNullEngine_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = Substitute.For<ILogger<BackgroundProcessingService>>();
        var options = Options.Create(new BackgroundProcessingOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new BackgroundProcessingService(null!, logger, options));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackgroundProcessing();
        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<WorkItemProcessingService>();
        var options = Options.Create(new BackgroundProcessingOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new BackgroundProcessingService(engine, null!, options));
    }

    [Fact]
    public void Constructor_RegistersHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackgroundProcessing()
            .AddHandler<TestWorkItemHandler>("Test", 5, WorkItemHandlerPurpose.Internal);
        
        var serviceProvider = services.BuildServiceProvider();

        // Act - Engine initialization happens during GetRequiredService
        var engine = serviceProvider.GetRequiredService<WorkItemProcessingService>();

        // Assert - Engine should have handler registered and ready to use
        var handlers = engine.GetHandlers(WorkItemHandlerPurpose.Internal).ToList();
        Assert.Single(handlers);
        Assert.Contains("Test", handlers);
    }

    [Fact]
    public async Task ExecuteAsync_CallsProcessCycleAsync()
    {
        // Arrange
        var processCalled = false;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<TestProcessTracker>(sp => new TestProcessTracker { OnProcess = () => processCalled = true });
        services.AddBackgroundProcessing()
            .AddHandler<TrackedProcessingWorkItemHandler>("Test", 5, WorkItemHandlerPurpose.Internal);
        
        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<WorkItemProcessingService>();
        var logger = Substitute.For<ILogger<BackgroundProcessingService>>();
        var options = Options.Create(new BackgroundProcessingOptions
        {
            PollInterval = TimeSpan.FromMilliseconds(10)
        });

        var service = new BackgroundProcessingService(engine, logger, options);
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(150);
        await service.StopAsync(CancellationToken.None);

        // Assert - ProcessCycleAsync should have been called (handler was fetched)
        Assert.True(processCalled);
    }

    [Fact]
    public async Task ExecuteAsync_WithHandlerThrowingException_ContinuesProcessing()
    {
        // Arrange
        var callCount = 0;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<TestFailureTracker>(sp => new TestFailureTracker { OnFetch = () => callCount++ });
        services.AddBackgroundProcessing()
            .AddHandler<FailingFetchWorkItemHandler>("Test", 5, WorkItemHandlerPurpose.Internal);
        
        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<WorkItemProcessingService>();
        var logger = Substitute.For<ILogger<BackgroundProcessingService>>();
        var options = Options.Create(new BackgroundProcessingOptions
        {
            PollInterval = TimeSpan.FromMilliseconds(20)
        });

        var service = new BackgroundProcessingService(engine, logger, options);
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(150);
        await service.StopAsync(CancellationToken.None);

        // Assert - Should have tried multiple times despite exceptions
        Assert.True(callCount > 1, $"Expected multiple fetch attempts, got {callCount}");
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_StopsGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackgroundProcessing()
            .AddHandler<TestWorkItemHandler>("Test", 5, WorkItemHandlerPurpose.Internal);
        
        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<WorkItemProcessingService>();
        var logger = Substitute.For<ILogger<BackgroundProcessingService>>();
        var options = Options.Create(new BackgroundProcessingOptions
        {
            PollInterval = TimeSpan.FromMilliseconds(10)
        });

        var service = new BackgroundProcessingService(engine, logger, options);

        // Act
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(50);
        await service.StopAsync(CancellationToken.None);

        // Assert - Should complete without throwing (graceful shutdown)
    }

    // Test helper classes
    public class TestInitializationTracker
    {
        public Action? OnInitialize { get; set; }
    }

    public class TestProcessTracker
    {
        public Action? OnProcess { get; set; }
    }

    public class TestFailureTracker
    {
        public Action? OnFetch { get; set; }
    }

    private class TestWorkItem : IWorkItem
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    private class TestWorkItemHandler : IWorkItemHandler<TestWorkItem>
    {
        public Task<IReadOnlyCollection<TestWorkItem>> FetchNextAsync(int maxItems, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<TestWorkItem>>(Array.Empty<TestWorkItem>());
        }

        public Task ProcessAsync(TestWorkItem workItem, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private class TrackedTestWorkItemHandler : IWorkItemHandler<TestWorkItem>
    {
        private readonly TestInitializationTracker _tracker;

        public TrackedTestWorkItemHandler(TestInitializationTracker tracker)
        {
            _tracker = tracker;
            _tracker.OnInitialize?.Invoke();
        }

        public Task<IReadOnlyCollection<TestWorkItem>> FetchNextAsync(int maxItems, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<TestWorkItem>>(Array.Empty<TestWorkItem>());
        }

        public Task ProcessAsync(TestWorkItem workItem, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private class TrackedProcessingWorkItemHandler : IWorkItemHandler<TestWorkItem>
    {
        private readonly TestProcessTracker _tracker;

        public TrackedProcessingWorkItemHandler(TestProcessTracker tracker)
        {
            _tracker = tracker;
        }

        public Task<IReadOnlyCollection<TestWorkItem>> FetchNextAsync(int maxItems, CancellationToken cancellationToken)
        {
            _tracker.OnProcess?.Invoke();
            return Task.FromResult<IReadOnlyCollection<TestWorkItem>>(Array.Empty<TestWorkItem>());
        }

        public Task ProcessAsync(TestWorkItem workItem, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private class FailingFetchWorkItemHandler : IWorkItemHandler<TestWorkItem>
    {
        private readonly TestFailureTracker _tracker;

        public FailingFetchWorkItemHandler(TestFailureTracker tracker)
        {
            _tracker = tracker;
        }

        public Task<IReadOnlyCollection<TestWorkItem>> FetchNextAsync(int maxItems, CancellationToken cancellationToken)
        {
            _tracker.OnFetch?.Invoke();
            throw new InvalidOperationException("Simulated fetch failure");
        }

        public Task ProcessAsync(TestWorkItem workItem, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
