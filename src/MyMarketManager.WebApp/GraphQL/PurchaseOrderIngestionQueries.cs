using MyMarketManager.Data.Enums;
using MyMarketManager.Data.Processing;

namespace MyMarketManager.WebApp.GraphQL;

/// <summary>
/// GraphQL queries for PO ingestion
/// </summary>
[ExtendObjectType("Query")]
public class PurchaseOrderIngestionQueries
{
    /// <summary>
    /// Get available scrapers for a batch type
    /// </summary>
    public IEnumerable<string> GetAvailableScrapers(
        StagingBatchType batchType,
        [Service] IBatchProcessorFactory factory)
    {
        return factory.GetAvailableProcessors(batchType);
    }

    /// <summary>
    /// Get available scrapers for a batch type filtered by purpose
    /// </summary>
    public IEnumerable<string> GetAvailableScrapersForPurpose(
        StagingBatchType batchType,
        ProcessorPurpose purpose,
        [Service] IBatchProcessorFactory factory)
    {
        return factory.GetAvailableProcessors(batchType, purpose);
    }

    /// <summary>
    /// Get processor metadata by name
    /// </summary>
    public ProcessorMetadataDto? GetProcessorMetadata(
        string processorName,
        [Service] IBatchProcessorFactory factory)
    {
        var metadata = factory.GetProcessorMetadata(processorName);
        if (metadata == null)
        {
            return null;
        }

        return new ProcessorMetadataDto(
            metadata.BatchType,
            metadata.ProcessorType.Name,
            metadata.Purpose,
            metadata.DisplayName,
            metadata.Description);
    }
}

/// <summary>
/// Processor metadata for GraphQL
/// </summary>
public record ProcessorMetadataDto(
    StagingBatchType BatchType,
    string ProcessorTypeName,
    ProcessorPurpose Purpose,
    string? DisplayName,
    string? Description);

