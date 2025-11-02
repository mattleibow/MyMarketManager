using Microsoft.Extensions.DependencyInjection;
using MyMarketManager.Data.Processing;

namespace MyMarketManager.Data.Processing.Tests;

public class WorkItemProcessingExtensionsTests
{
    [Fact]
    public void AddWorkItemProcessing_RegistersEngine()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLogging();

        // Act
        var builder = services.AddWorkItemProcessing();

        // Assert
        Assert.NotNull(builder);
        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetService<WorkItemProcessingEngine>();
        Assert.NotNull(engine);
    }

    [Fact]
    public void AddWorkItemProcessing_ReturnsBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var builder = services.AddWorkItemProcessing();

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
        var builder = services.AddWorkItemProcessing();

        // Act
        builder.AddHandler<TestWorkItemHandler, TestWorkItem>("Test", 5, ProcessorPurpose.Internal);

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
        var builder = services.AddWorkItemProcessing();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            builder.AddHandler<TestWorkItemHandler, TestWorkItem>(null!, 5, ProcessorPurpose.Internal));
    }

    [Fact]
    public void AddHandler_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddWorkItemProcessing();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            builder.AddHandler<TestWorkItemHandler, TestWorkItem>("", 5, ProcessorPurpose.Internal));
    }

    [Fact]
    public void AddHandler_WithWhitespaceName_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddWorkItemProcessing();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            builder.AddHandler<TestWorkItemHandler, TestWorkItem>("   ", 5, ProcessorPurpose.Internal));
    }

    [Fact]
    public void AddHandler_WithZeroMaxItems_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddWorkItemProcessing();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            builder.AddHandler<TestWorkItemHandler, TestWorkItem>("Test", 0, ProcessorPurpose.Internal));
    }

    [Fact]
    public void AddHandler_WithNegativeMaxItems_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddWorkItemProcessing();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            builder.AddHandler<TestWorkItemHandler, TestWorkItem>("Test", -1, ProcessorPurpose.Internal));
    }

    [Fact]
    public void AddHandler_SupportsChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var builder = services.AddWorkItemProcessing()
            .AddHandler<TestWorkItemHandler, TestWorkItem>("Test1", 5, ProcessorPurpose.Internal)
            .AddHandler<AnotherTestWorkItemHandler, TestWorkItem>("Test2", 10, ProcessorPurpose.Ingestion);

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
        services.AddWorkItemProcessing()
            .AddHandler<TestWorkItemHandler, TestWorkItem>("Ingestion", 5, ProcessorPurpose.Ingestion)
            .AddHandler<AnotherTestWorkItemHandler, TestWorkItem>("Internal", 10, ProcessorPurpose.Internal)
            .AddHandler<ThirdTestWorkItemHandler, TestWorkItem>("Export", 15, ProcessorPurpose.Export);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<WorkItemProcessingEngine>();
        engine.Initialize();

        var ingestionHandlers = engine.GetHandlerNamesByPurpose(ProcessorPurpose.Ingestion).ToList();
        var internalHandlers = engine.GetHandlerNamesByPurpose(ProcessorPurpose.Internal).ToList();
        var exportHandlers = engine.GetHandlerNamesByPurpose(ProcessorPurpose.Export).ToList();

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
        services.AddWorkItemProcessing()
            .AddHandler<TestWorkItemHandler, TestWorkItem>("Test");

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
        services.AddWorkItemProcessing()
            .AddHandler<TestWorkItemHandler, TestWorkItem>("Test", 5);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<WorkItemProcessingEngine>();
        engine.Initialize();

        var internalHandlers = engine.GetHandlerNamesByPurpose(ProcessorPurpose.Internal).ToList();
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
}
