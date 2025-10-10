using System.ComponentModel.DataAnnotations;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Entities;

/// <summary>
/// Represents a single supplier data upload (e.g. Shein ZIP) or sales data upload (e.g. Yoco API load), grouping all parsed orders, sales and items.
/// </summary>
public class StagingBatch : EntityBase
{
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public DateTimeOffset UploadDate { get; set; }

    [Required]
    public string FileHash { get; set; } = string.Empty;
    public ProcessingStatus Status { get; set; }
    public string? Notes { get; set; }

    /// <summary>
    /// Error message if the batch processing failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    // Navigation properties
    public ICollection<StagingPurchaseOrder> StagingPurchaseOrders { get; set; } = new List<StagingPurchaseOrder>();
    public ICollection<StagingSale> StagingSales { get; set; } = new List<StagingSale>();
    public ICollection<ScraperSession> ScraperSessions { get; set; } = new List<ScraperSession>();
}
