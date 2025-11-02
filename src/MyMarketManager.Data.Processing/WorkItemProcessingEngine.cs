using System.Reflection.Metadata;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

    public WorkItemProcessingEngine(
        IServiceProvider serviceProvider,
        ILogger<WorkItemProcessingEngine> logger,
        IOptions<WorkItemProcessingEngineOptions> options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        ArgumentNullException.ThrowIfNull(options);
        
        // Register all handlers from options
        foreach (var registration in options.Value.Registrations)
        {
            RegisterHandlerInternal(
                registration.HandlerType,
                registration.WorkItemType,
                registration.Name,
                registration.MaxItemsPerCycle,
                registration.Purpose);
        }
        
        _logger.LogInformation("WorkItemProcessingEngine initialized with {Count} handlers", _registrations.Count);
    }

    private void RegisterHandlerInternal(
        Type handlerType,
        Type workItemType,
        string name,
        int maxItemsPerCycle,
        ProcessorPurpose purpose)
    {
        var registrationType = typeof(WorkItemHandlerRegistration<>).MakeGenericType(workItemType);
        var registration = (IWorkItemHandlerRegistration)Activator.CreateInstance(
            registrationType,
            handlerType,
            name,
            maxItemsPerCycle,
            purpose)!;
        
        _registrations.Add(registration);
        
        _logger.LogInformation(
            "Registered handler: {HandlerType} as '{Name}' for work item type {WorkItemType} (Max: {MaxItems}, Purpose: {Purpose})",
            handlerType.Name,
            name,
            workItemType.Name,
            maxItemsPerCycle,
            purpose);
    }

    /// <summary>
    /// Gets all registered handler names by purpose.
    /// </summary>
    public IEnumerable<string> GetHandlerNamesByPurpose(ProcessorPurpose purpose)
    {
        return _registrations
            .Where(r => r.GetPurpose() == purpose)
            .Select(r => r.GetName());
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
        var channel = Channel.CreateBounded<IWorkItemEnvelope>(new BoundedChannelOptions(totalCapacity)
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

    private async Task FetchWorkItemsAsync(ChannelWriter<IWorkItemEnvelope> writer, CancellationToken cancellationToken)
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

    private async Task ProcessWorkItemsAsync(ChannelReader<IWorkItemEnvelope> reader, CancellationToken cancellationToken)
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

    private async Task ProcessWorkItemAsync(IWorkItemEnvelope envelope, CancellationToken cancellationToken)
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
        Task FetchAndEnqueueAsync(IServiceProvider serviceProvider, ChannelWriter<IWorkItemEnvelope> writer, ILogger logger, CancellationToken cancellationToken);
        int GetMaxItemsPerCycle();
        string GetName();
        ProcessorPurpose GetPurpose();
    }

    private class WorkItemHandlerRegistration<TWorkItem> : IWorkItemHandlerRegistration
        where TWorkItem : IWorkItem
    {
        private readonly Type _handlerType;
        private readonly string _name;
        private readonly int _maxItemsPerCycle;
        private readonly ProcessorPurpose _purpose;

        public WorkItemHandlerRegistration(
            Type handlerType, 
            string name, 
            int maxItemsPerCycle, 
            ProcessorPurpose purpose)
        {
            _handlerType = handlerType;
            _name = name;
            _maxItemsPerCycle = maxItemsPerCycle;
            _purpose = purpose;
        }

        public int GetMaxItemsPerCycle() => _maxItemsPerCycle;
        public string GetName() => _name;
        public ProcessorPurpose GetPurpose() => _purpose;

        public async Task FetchAndEnqueueAsync(
            IServiceProvider serviceProvider, 
            ChannelWriter<IWorkItemEnvelope> writer,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var handler = (IWorkItemHandler<TWorkItem>)serviceProvider.GetRequiredService(_handlerType);

            logger.LogDebug("Fetching up to {MaxItems} items from handler '{Name}'", _maxItemsPerCycle, _name);
            
            var workItems = await handler.FetchWorkItemsAsync(_maxItemsPerCycle, cancellationToken);

            if (workItems.Count > _maxItemsPerCycle)
            {
                logger.LogWarning(
                    "Handler '{Name}' returned {ActualCount} items but max is {MaxItems}. Truncating.",
                    _name, workItems.Count, _maxItemsPerCycle);
                workItems = workItems.Take(_maxItemsPerCycle).ToList();
            }

            if (workItems.Count > 0)
            {
                logger.LogInformation("Handler '{Name}' fetched {Count} work items", _name, workItems.Count);

                foreach (var workItem in workItems)
                {
                    var envelope = new WorkItemEnvelope<TWorkItem>(_name, _handlerType, workItem);
                    await writer.WriteAsync(envelope, cancellationToken);
                }
            }
        }
    }

    private interface IWorkItemEnvelope
    {
        string HandlerName { get; }

        Guid WorkItemId { get; }

        Task ProcessAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken);
    }

    private class WorkItemEnvelope<TWorkItem>(string handlerName, Type handlerType, TWorkItem workItem) : IWorkItemEnvelope
        where TWorkItem : IWorkItem
    {
        public string HandlerName => handlerName;

        public Guid WorkItemId => workItem.Id;

        public async Task ProcessAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var handler = (IWorkItemHandler<TWorkItem>)serviceProvider.GetRequiredService(handlerType);
            await handler.ProcessAsync(workItem, cancellationToken);
        }
    }
}
