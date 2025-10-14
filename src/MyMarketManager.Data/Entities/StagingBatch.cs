using System.ComponentModel.DataAnnotations;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Entities;

/// <summary>
/// Represents a single data upload (e.g. Shein ZIP, sales data upload), grouping all parsed orders, sales and items.
/// The source (supplier, store, etc.) is tracked through related entities like ScraperSession.
/// </summary>
public class StagingBatch : EntityBase
{
    public DateTimeOffset UploadDate { get; set; }

    [Required]
    public string FileHash { get; set; } = string.Empty;
    public ProcessingStatus Status { get; set; }
    public string? Notes { get; set; }

    /// <summary>
    /// Error message if the batch processing failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The scraper session that created this batch, if any.
    /// </summary>
    public Guid? ScraperSessionId { get; set; }
    public ScraperSession? ScraperSession { get; set; }

    // Navigation properties
    public ICollection<StagingPurchaseOrder> StagingPurchaseOrders { get; set; } = new List<StagingPurchaseOrder>();
    public ICollection<StagingSale> StagingSales { get; set; } = new List<StagingSale>();
}
