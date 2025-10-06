using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Entities;

/// <summary>
/// Line items from a supplier order in staging, awaiting linking or confirmation.
/// </summary>
public class StagingPurchaseOrderItem
{
    public int Id { get; set; }
    public int StagingPurchaseOrderId { get; set; }
    public int? ProductId { get; set; }
    public int? SupplierId { get; set; }
    public int? PurchaseOrderItemId { get; set; }
    public string SupplierReference { get; set; } = string.Empty;
    public string? SupplierProductUrl { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public decimal ListedUnitPrice { get; set; }
    public decimal ActualUnitPrice { get; set; }
    public string RawData { get; set; } = string.Empty;
    public bool IsImported { get; set; }
    public CandidateStatus Status { get; set; }

    // Navigation properties
    public StagingPurchaseOrder StagingPurchaseOrder { get; set; } = null!;
    public Product? Product { get; set; }
    public Supplier? Supplier { get; set; }
    public PurchaseOrderItem? PurchaseOrderItem { get; set; }
}
