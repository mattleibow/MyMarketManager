using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Processing;

/// <summary>
/// Factory that creates processors based on processor name.
/// Supports both legacy IBatchProcessor and generic IWorkItemProcessor.
/// </summary>
internal class BatchProcessorFactory(IServiceProvider serviceProvider, IOptions<BatchProcessorOptions> options) : IBatchProcessorFactory
{
    /// <summary>
    /// Gets a batch processor for the given processor name.
    /// Returns null if the processor is not an IBatchProcessor.
    /// </summary>
    public IBatchProcessor? GetProcessor(string processorName)
    {
        if (string.IsNullOrWhiteSpace(processorName))
        {
            return null;
        }

        var processors = options.Value.Processors;
        if (!processors.TryGetValue(processorName, out var metadata))
        {
            return null;
        }

        // Only return if it's an IBatchProcessor
        if (!typeof(IBatchProcessor).IsAssignableFrom(metadata.ProcessorType))
        {
            return null;
        }

        return serviceProvider.GetRequiredService(metadata.ProcessorType) as IBatchProcessor;
    }

    /// <summary>
    /// Gets a generic work item processor for the given processor name and work item type.
    /// </summary>
    public object? GetWorkItemProcessor(string processorName, Type workItemType)
    {
        if (string.IsNullOrWhiteSpace(processorName) || workItemType == null)
        {
            return null;
        }

        var processors = options.Value.Processors;
        if (!processors.TryGetValue(processorName, out var metadata))
        {
            return null;
        }

        // Verify the work item type matches
        if (metadata.WorkItemType != workItemType)
        {
            return null;
        }

        return serviceProvider.GetRequiredService(metadata.ProcessorType);
    }

    /// <summary>
    /// Gets all available processor names for a given batch type.
    /// Note: This only returns processors that handle StagingBatch work items.
    /// Work item processors with null BatchType (like ImageVectorization) are excluded.
    /// </summary>
    public IEnumerable<string> GetAvailableProcessors(StagingBatchType batchType)
    {
        return options.Value.Processors
            .Where(kvp => kvp.Value.BatchType == batchType)
            .Select(kvp => kvp.Key);
    }

    /// <summary>
    /// Gets all available processor names for a given purpose.
    /// </summary>
    public IEnumerable<string> GetProcessorsByPurpose(ProcessorPurpose purpose)
    {
        return options.Value.Processors
            .Where(kvp => kvp.Value.Purpose == purpose)
            .Select(kvp => kvp.Key);
    }

    /// <summary>
    /// Gets all available processor names for a given batch type and purpose.
    /// </summary>
    public IEnumerable<string> GetAvailableProcessors(StagingBatchType batchType, ProcessorPurpose purpose)
    {
        return options.Value.Processors
            .Where(kvp => kvp.Value.BatchType == batchType && kvp.Value.Purpose == purpose)
            .Select(kvp => kvp.Key);
    }

    /// <summary>
    /// Gets metadata for a specific processor.
    /// </summary>
    public ProcessorMetadata? GetProcessorMetadata(string processorName)
    {
        if (string.IsNullOrWhiteSpace(processorName))
        {
            return null;
        }

        return options.Value.Processors.TryGetValue(processorName, out var metadata) ? metadata : null;
    }
}

