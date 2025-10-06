using System.ComponentModel.DataAnnotations;

namespace MyMarketManager.Data.Entities;

/// <summary>
/// Line items within a purchase order, representing specific products or SKUs ordered.
/// </summary>
public class PurchaseOrderItem
{
    public Guid Id { get; set; }
    
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }
    
    [Required]
    public string SupplierReference { get; set; } = string.Empty;
    public string? SupplierProductUrl { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Quantity { get; set; }
    
    public decimal ListedUnitPrice { get; set; }
    public decimal ActualUnitPrice { get; set; }
    public decimal AllocatedUnitOverhead { get; set; }
    public decimal TotalUnitCost { get; set; }

    // Navigation properties
    public ICollection<StagingPurchaseOrderItem> StagingPurchaseOrderItems { get; set; } = new List<StagingPurchaseOrderItem>();
}
