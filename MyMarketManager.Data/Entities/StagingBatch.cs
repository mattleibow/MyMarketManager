using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Entities;

/// <summary>
/// Represents a single supplier data upload (e.g. Shein ZIP) or sales data upload (e.g. Yoco API load), grouping all parsed orders, sales and items.
/// </summary>
public class StagingBatch
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public DateTime UploadDate { get; set; }
    public string FileHash { get; set; } = string.Empty;
    public ProcessingStatus Status { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public Supplier Supplier { get; set; } = null!;
    public ICollection<StagingPurchaseOrder> StagingPurchaseOrders { get; set; } = new List<StagingPurchaseOrder>();
    public ICollection<StagingSale> StagingSales { get; set; } = new List<StagingSale>();
}
