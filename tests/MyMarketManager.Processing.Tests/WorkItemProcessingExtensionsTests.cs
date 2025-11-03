using Microsoft.Extensions.DependencyInjection;
using MyMarketManager.Processing;

namespace MyMarketManager.Processing.Tests;

public class WorkItemProcessingExtensionsTests
{
    [Fact]
    public void AddWorkItemProcessing_RegistersProcessingService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLogging();

        // Act
        var builder = services.AddBackgroundProcessing();

        // Assert
        Assert.NotNull(builder);
        var serviceProvider = services.BuildServiceProvider();
        var processingService = serviceProvider.GetService<WorkItemProcessingService>();
        Assert.NotNull(processingService);
    }

    [Fact]
    public void AddWorkItemProcessing_ReturnsBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var builder = services.AddBackgroundProcessing();

        // Assert
        Assert.NotNull(builder);
        Assert.NotNull(builder.Services);
        Assert.Same(services, builder.Services);
    }

    [Fact]
    public void AddHandler_RegistersHandlerAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddBackgroundProcessing();

        // Act
        builder.AddHandler<TestWorkItemHandler>("Test", 5, WorkItemHandlerPurpose.Internal);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        using var scope1 = serviceProvider.CreateScope();
        using var scope2 = serviceProvider.CreateScope();
        
        var handler1 = scope1.ServiceProvider.GetService<TestWorkItemHandler>();
        var handler2 = scope2.ServiceProvider.GetService<TestWorkItemHandler>();
        
        Assert.NotNull(handler1);
        Assert.NotNull(handler2);
        Assert.NotSame(handler1, handler2); // Different instances per scope
    }

    [Fact]
    public void AddHandler_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddBackgroundProcessing();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            builder.AddHandler<TestWorkItemHandler>(null!, 5, WorkItemHandlerPurpose.Internal));
    }

    [Fact]
    public void AddHandler_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddBackgroundProcessing();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            builder.AddHandler<TestWorkItemHandler>("", 5, WorkItemHandlerPurpose.Internal));
    }

    [Fact]
    public void AddHandler_WithWhitespaceName_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddBackgroundProcessing();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            builder.AddHandler<TestWorkItemHandler>("   ", 5, WorkItemHandlerPurpose.Internal));
    }

    [Fact]
    public void AddHandler_WithZeroMaxItems_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddBackgroundProcessing();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            builder.AddHandler<TestWorkItemHandler>("Test", 0, WorkItemHandlerPurpose.Internal));
    }

    [Fact]
    public void AddHandler_WithNegativeMaxItems_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddBackgroundProcessing();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            builder.AddHandler<TestWorkItemHandler>("Test", -1, WorkItemHandlerPurpose.Internal));
    }

    [Fact]
    public void AddHandler_SupportsChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var builder = services.AddBackgroundProcessing()
            .AddHandler<TestWorkItemHandler>("Test1", 5, WorkItemHandlerPurpose.Internal)
            .AddHandler<AnotherTestWorkItemHandler>("Test2", 10, WorkItemHandlerPurpose.Ingestion);

        // Assert
        Assert.NotNull(builder);
        var serviceProvider = services.BuildServiceProvider();
        
        var handler1 = serviceProvider.CreateScope().ServiceProvider.GetService<TestWorkItemHandler>();
        var handler2 = serviceProvider.CreateScope().ServiceProvider.GetService<AnotherTestWorkItemHandler>();
        
        Assert.NotNull(handler1);
        Assert.NotNull(handler2);
    }

    [Fact]
    public void AddHandler_WithDifferentPurposes_RegistersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddBackgroundProcessing()
            .AddHandler<TestWorkItemHandler>("Ingestion", 5, WorkItemHandlerPurpose.Ingestion)
            .AddHandler<AnotherTestWorkItemHandler>("Internal", 10, WorkItemHandlerPurpose.Internal)
            .AddHandler<ThirdTestWorkItemHandler>("Export", 15, WorkItemHandlerPurpose.Export);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var processingService = serviceProvider.GetRequiredService<WorkItemProcessingService>();

        var ingestionHandlers = processingService.GetHandlers(WorkItemHandlerPurpose.Ingestion).ToList();
        var internalHandlers = processingService.GetHandlers(WorkItemHandlerPurpose.Internal).ToList();
        var exportHandlers = processingService.GetHandlers(WorkItemHandlerPurpose.Export).ToList();

        Assert.Single(ingestionHandlers);
        Assert.Contains("Ingestion", ingestionHandlers);
        
        Assert.Single(internalHandlers);
        Assert.Contains("Internal", internalHandlers);
        
        Assert.Single(exportHandlers);
        Assert.Contains("Export", exportHandlers);
    }

    [Fact]
    public void AddHandler_WithDefaultMaxItems_UsesDefaultValue()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddBackgroundProcessing()
            .AddHandler<TestWorkItemHandler>("Test");

        // Assert - If this doesn't throw, the default of 10 was accepted
        var serviceProvider = services.BuildServiceProvider();
        var handler = serviceProvider.CreateScope().ServiceProvider.GetService<TestWorkItemHandler>();
        Assert.NotNull(handler);
    }

    [Fact]
    public void AddHandler_WithDefaultPurpose_UsesInternalPurpose()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddBackgroundProcessing()
            .AddHandler<TestWorkItemHandler>("Test", 5);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var processingService = serviceProvider.GetRequiredService<WorkItemProcessingService>();

        var internalHandlers = processingService.GetHandlers(WorkItemHandlerPurpose.Internal).ToList();
        Assert.Single(internalHandlers);
        Assert.Contains("Test", internalHandlers);
    }

    // Test helper classes
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

    private class AnotherTestWorkItemHandler : IWorkItemHandler<TestWorkItem>
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

    private class ThirdTestWorkItemHandler : IWorkItemHandler<TestWorkItem>
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
}
