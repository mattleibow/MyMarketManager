namespace MyMarketManager.Data.Entities;

/// <summary>
/// Line items within a purchase order, representing specific products or SKUs ordered.
/// </summary>
public class PurchaseOrderItem
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public int? ProductId { get; set; }
    public string SupplierReference { get; set; } = string.Empty;
    public string? SupplierProductUrl { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public decimal ListedUnitPrice { get; set; }
    public decimal ActualUnitPrice { get; set; }
    public decimal AllocatedUnitOverhead { get; set; }
    public decimal TotalUnitCost { get; set; }

    // Navigation properties
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public Product? Product { get; set; }
    public ICollection<StagingPurchaseOrderItem> StagingPurchaseOrderItems { get; set; } = new List<StagingPurchaseOrderItem>();
}
