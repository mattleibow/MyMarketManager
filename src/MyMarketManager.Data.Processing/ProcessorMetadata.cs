using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Processing;

/// <summary>
/// Metadata about a registered processor (batch or work item processor).
/// </summary>
public class ProcessorMetadata
{
    /// <summary>
    /// The batch type this processor handles (for StagingBatch processors).
    /// Null for non-StagingBatch processors.
    /// </summary>
    public StagingBatchType? BatchType { get; init; }

    /// <summary>
    /// The work item type this processor handles.
    /// </summary>
    public required Type WorkItemType { get; init; }

    /// <summary>
    /// The processor implementation type.
    /// </summary>
    public required Type ProcessorType { get; init; }

    /// <summary>
    /// The purpose/category of this processor.
    /// Defaults to Ingestion for backward compatibility.
    /// </summary>
    public ProcessorPurpose Purpose { get; init; } = ProcessorPurpose.Ingestion;

    /// <summary>
    /// Optional display name for UI.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Optional description for UI.
    /// </summary>
    public string? Description { get; init; }
}

