using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MyMarketManager.Data.Processing;

/// <summary>
/// Central processing engine that orchestrates work item processing using System.Threading.Channels.
/// Features:
/// - Each registered handler fetches its own work items
/// - Bounded channels prevent memory issues
/// - Fair scheduling prevents starvation
/// - Single background service for all processors
/// </summary>
public class WorkItemProcessingEngine
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkItemProcessingEngine> _logger;
    private readonly List<IWorkItemHandlerRegistration> _registrations = new();
    private bool _initialized = false;

    public WorkItemProcessingEngine(
        IServiceProvider serviceProvider,
        ILogger<WorkItemProcessingEngine> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes the engine with all registered handlers.
    /// Should be called once during startup.
    /// </summary>
    public void Initialize()
    {
        if (_initialized)
            return;

        WorkItemProcessingExtensions.ConfigureEngine(_serviceProvider, this);
        _initialized = true;
        _logger.LogInformation("WorkItemProcessingEngine initialized with {Count} handlers", _registrations.Count);
    }

    /// <summary>
    /// Registers a work item handler for processing.
    /// </summary>
    public void RegisterHandler<TWorkItem>(Type handlerType) where TWorkItem : IWorkItem
    {
        _registrations.Add(new WorkItemHandlerRegistration<TWorkItem>(handlerType));
        _logger.LogInformation("Registered handler: {HandlerType} for work item type {WorkItemType}", 
            handlerType.Name, typeof(TWorkItem).Name);
    }

    /// <summary>
    /// Processes one cycle of work items from all registered handlers.
    /// This method:
    /// 1. Fetches work from each handler (respecting max items)
    /// 2. Queues all work items into a channel
    /// 3. Processes them concurrently
    /// </summary>
    public async Task ProcessCycleAsync(CancellationToken cancellationToken)
    {
        if (_registrations.Count == 0)
        {
            _logger.LogDebug("No handlers registered, skipping cycle");
            return;
        }

        // Create a bounded channel to hold all work items for this cycle
        // Capacity = sum of max items from all handlers
        var totalCapacity = _registrations.Sum(r => r.GetMaxItemsPerCycle());
        var channel = Channel.CreateBounded<WorkItemEnvelope>(new BoundedChannelOptions(totalCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false, // Multiple consumers
            SingleWriter = true   // Only this method writes
        });

        try
        {
            // Phase 1: Fetch work items from all handlers
            _logger.LogDebug("Starting fetch phase for {HandlerCount} handlers", _registrations.Count);
            await FetchWorkItemsAsync(channel.Writer, cancellationToken);
            channel.Writer.Complete();

            // Phase 2: Process all queued work items
            _logger.LogDebug("Starting processing phase");
            await ProcessWorkItemsAsync(channel.Reader, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during processing cycle");
            throw;
        }
    }

    private async Task FetchWorkItemsAsync(ChannelWriter<WorkItemEnvelope> writer, CancellationToken cancellationToken)
    {
        var fetchTasks = new List<Task>();

        using var scope = _serviceProvider.CreateScope();

        foreach (var registration in _registrations)
        {
            // Fetch from each handler in parallel
            var task = registration.FetchAndEnqueueAsync(scope.ServiceProvider, writer, _logger, cancellationToken);
            fetchTasks.Add(task);
        }

        await Task.WhenAll(fetchTasks);
    }

    private async Task ProcessWorkItemsAsync(ChannelReader<WorkItemEnvelope> reader, CancellationToken cancellationToken)
    {
        var processingTasks = new List<Task>();
        var processedCount = 0;

        // Read all available work items and process them concurrently
        await foreach (var envelope in reader.ReadAllAsync(cancellationToken))
        {
            var task = ProcessWorkItemAsync(envelope, cancellationToken);
            processingTasks.Add(task);
            processedCount++;
        }

        if (processedCount > 0)
        {
            _logger.LogInformation("Processing {Count} work items across all handlers", processedCount);
            await Task.WhenAll(processingTasks);
            _logger.LogInformation("Completed processing {Count} work items", processedCount);
        }
    }

    private async Task ProcessWorkItemAsync(WorkItemEnvelope envelope, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        
        try
        {
            await envelope.ProcessAsync(scope.ServiceProvider, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing work item {WorkItemId} from handler {HandlerName}", 
                envelope.WorkItemId, envelope.HandlerName);
        }
    }

    // Internal types for registration and envelope

    private interface IWorkItemHandlerRegistration
    {
        Task FetchAndEnqueueAsync(IServiceProvider serviceProvider, ChannelWriter<WorkItemEnvelope> writer, ILogger logger, CancellationToken cancellationToken);
        int GetMaxItemsPerCycle();
    }

    private class WorkItemHandlerRegistration<TWorkItem> : IWorkItemHandlerRegistration where TWorkItem : IWorkItem
    {
        private readonly Type _handlerType;

        public WorkItemHandlerRegistration(Type handlerType)
        {
            _handlerType = handlerType;
        }

        public int GetMaxItemsPerCycle()
        {
            // Default to 10, will be read from handler instance when fetching
            return 10;
        }

        public async Task FetchAndEnqueueAsync(
            IServiceProvider serviceProvider, 
            ChannelWriter<WorkItemEnvelope> writer,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var handler = (IWorkItemHandler<TWorkItem>)serviceProvider.GetRequiredService(_handlerType);
            var maxItems = handler.MaxItemsPerCycle;

            logger.LogDebug("Fetching up to {MaxItems} items from handler {HandlerName}", maxItems, handler.Name);
            
            var workItems = await handler.FetchWorkItemsAsync(maxItems, cancellationToken);

            if (workItems.Count > maxItems)
            {
                logger.LogWarning(
                    "Handler {HandlerName} returned {ActualCount} items but max is {MaxItems}. Truncating.",
                    handler.Name, workItems.Count, maxItems);
                workItems = workItems.Take(maxItems).ToList();
            }

            if (workItems.Count > 0)
            {
                logger.LogInformation("Handler {HandlerName} fetched {Count} work items", handler.Name, workItems.Count);

                foreach (var workItem in workItems)
                {
                    var envelope = new WorkItemEnvelope<TWorkItem>(handler.Name, _handlerType, workItem);
                    await writer.WriteAsync(envelope, cancellationToken);
                }
            }
        }
    }

    private abstract class WorkItemEnvelope
    {
        public abstract string HandlerName { get; }
        public abstract Guid WorkItemId { get; }
        public abstract Task ProcessAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken);
    }

    private class WorkItemEnvelope<TWorkItem> : WorkItemEnvelope where TWorkItem : IWorkItem
    {
        private readonly string _handlerName;
        private readonly Type _handlerType;
        private readonly TWorkItem _workItem;

        public WorkItemEnvelope(string handlerName, Type handlerType, TWorkItem workItem)
        {
            _handlerName = handlerName;
            _handlerType = handlerType;
            _workItem = workItem;
        }

        public override string HandlerName => _handlerName;
        public override Guid WorkItemId => _workItem.Id;

        public override async Task ProcessAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var handler = (IWorkItemHandler<TWorkItem>)serviceProvider.GetRequiredService(_handlerType);
            await handler.ProcessAsync(_workItem, cancellationToken);
        }
    }
}
