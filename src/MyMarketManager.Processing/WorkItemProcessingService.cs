using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MyMarketManager.Processing;

/// <summary>
/// Central processing service that orchestrates work item processing using System.Threading.Channels.
/// Features:
/// - Each registered handler fetches its own work items
/// - Bounded channels prevent memory issues
/// - Fair scheduling prevents starvation
/// - Single background service for all processors
/// </summary>
public class WorkItemProcessingService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkItemProcessingService> _logger;
    private readonly IReadOnlyList<WorkItemHandlerRegistration> _registrations;

    public WorkItemProcessingService(
        IServiceProvider serviceProvider,
        ILogger<WorkItemProcessingService> logger,
        IOptions<WorkItemProcessingServiceOptions> options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ = options ?? throw new ArgumentNullException(nameof(options));

        _registrations = [.. options.Value.Registrations];

        _logger.LogInformation("WorkItemProcessingService initialized with {HandlerCount} handlers",
            _registrations.Count);

        foreach (var registration in _registrations)
        {
            _logger.LogDebug(" - Handler '{Name}' of type {HandlerType} (MaxItemsPerCycle={MaxItems}, Purpose={Purpose})",
                registration.Name,
                registration.HandlerType.FullName,
                registration.MaxItemsPerCycle,
                registration.Purpose);
        }
    }

    /// <summary>
    /// Gets all registered handler names by purpose.
    /// </summary>
    public IEnumerable<string> GetHandlers(WorkItemHandlerPurpose purpose) =>
        _registrations
            .Where(r => r.Purpose == purpose)
            .Select(r => r.Name);

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
        var totalCapacity = _registrations.Sum(r => r.MaxItemsPerCycle);
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

            await FetchNextAsync(channel.Writer, cancellationToken);
            channel.Writer.Complete();

            // Phase 2: Process all queued work items
            _logger.LogDebug("Starting processing phase");

            await ProcessAsync(channel.Reader, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during processing cycle");
            throw;
        }
    }

    private async Task FetchNextAsync(ChannelWriter<WorkItemEnvelope> writer, CancellationToken cancellationToken)
    {
        var fetchTasks = new List<Task>();

        using var scope = _serviceProvider.CreateScope();

        foreach (var registration in _registrations)
        {
            // Fetch from each handler in parallel
            var task = FetchAndEnqueueAsync(registration, scope.ServiceProvider, writer, cancellationToken);
            fetchTasks.Add(task);
        }

        await Task.WhenAll(fetchTasks);
    }

    private async Task FetchAndEnqueueAsync(
        WorkItemHandlerRegistration registration,
        IServiceProvider serviceProvider,
        ChannelWriter<WorkItemEnvelope> writer,
        CancellationToken cancellationToken)
    {
        var handler = (IWorkItemHandler)serviceProvider.GetRequiredService(registration.HandlerType);

        _logger.LogDebug("Fetching up to {MaxItems} items from handler '{Name}'", registration.MaxItemsPerCycle, registration.Name);

        var workItems = await handler.FetchNextAsync(registration.MaxItemsPerCycle, cancellationToken);

        if (workItems.Count > registration.MaxItemsPerCycle)
        {
            _logger.LogWarning(
                "Handler '{Name}' returned {ActualCount} items but max is {MaxItems}. Truncating.",
                registration.Name, workItems.Count, registration.MaxItemsPerCycle);

            workItems = workItems.Take(registration.MaxItemsPerCycle).ToList();
        }

        if (workItems.Count > 0)
        {
            _logger.LogInformation("Handler '{Name}' fetched {Count} work items", registration.Name, workItems.Count);

            foreach (var workItem in workItems)
            {
                var envelope = new WorkItemEnvelope(registration.Name, registration.HandlerType, workItem);
                await writer.WriteAsync(envelope, cancellationToken);
            }
        }
    }

    private async Task ProcessAsync(ChannelReader<WorkItemEnvelope> reader, CancellationToken cancellationToken)
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
            var handler = (IWorkItemHandler)scope.ServiceProvider.GetRequiredService(envelope.HandlerType);

            await handler.ProcessAsync(envelope.WorkItem, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing work item {WorkItemId} from handler {HandlerName}",
                envelope.WorkItem.Id, envelope.HandlerName);
        }
    }

    private record WorkItemEnvelope(
        string HandlerName,
        Type HandlerType,
        IWorkItem WorkItem);
}
