using System.ComponentModel.DataAnnotations;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Entities;

/// <summary>
/// A parsed supplier order stored in staging before validation and promotion.
/// </summary>
public class StagingPurchaseOrder : EntityBase
{
    public Guid StagingBatchId { get; set; }
    public StagingBatch StagingBatch { get; set; } = null!;

    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public string? SupplierReference { get; set; }
    public DateTimeOffset OrderDate { get; set; }

    [Required]
    public string RawData { get; set; } = string.Empty;
    public bool IsImported { get; set; }

    /// <summary>
    /// Status of the order scraping/processing.
    /// </summary>
    public ProcessingStatus Status { get; set; }

    /// <summary>
    /// Error message if the order scraping/processing failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    // Navigation properties
    public ICollection<StagingPurchaseOrderItem> Items { get; set; } = new List<StagingPurchaseOrderItem>();
}
