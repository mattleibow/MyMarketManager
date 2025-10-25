using System.ComponentModel.DataAnnotations;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Entities;

/// <summary>
/// Represents a single data upload (e.g. Shein ZIP, sales data upload, web scrape), grouping all parsed orders, sales and items.
/// </summary>
public class StagingBatch : EntityBase
{
    /// <summary>
    /// The type of staging batch (web scrape, blob upload, etc.).
    /// </summary>
    public StagingBatchType BatchType { get; set; }

    /// <summary>
    /// The name of the processor to use for this batch (e.g., "Shein" for web scraper, "SheinZipParser" for zip processor, "YocoApi" for sales data).
    /// Combined with BatchType to determine the exact processing logic.
    /// </summary>
    public string? BatchProcessorName { get; set; }

    /// <summary>
    /// The supplier this batch is for (for web scrapes).
    /// </summary>
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    /// <summary>
    /// When this batch started processing.
    /// </summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// When this batch completed processing (if successful).
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    [Required]
    public string FileHash { get; set; } = string.Empty;
    
    public ProcessingStatus Status { get; set; }
    
    public string? Notes { get; set; }

    /// <summary>
    /// Error message if the batch processing failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// File contents (e.g. serialized cookie JSON for web scrapes).
    /// </summary>
    public string? FileContents { get; set; }

    // Navigation properties
    public ICollection<StagingPurchaseOrder> StagingPurchaseOrders { get; set; } = new List<StagingPurchaseOrder>();
    public ICollection<StagingSale> StagingSales { get; set; } = new List<StagingSale>();
}
