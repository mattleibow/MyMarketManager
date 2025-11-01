using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Processing;

/// <summary>
/// Metadata about a registered batch processor.
/// </summary>
public class ProcessorMetadata
{
    /// <summary>
    /// The batch type this processor handles.
    /// </summary>
    public required StagingBatchType BatchType { get; init; }

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
