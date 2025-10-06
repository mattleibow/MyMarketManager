using System.ComponentModel.DataAnnotations;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Entities;

/// <summary>
/// Line items from a supplier order in staging, awaiting linking or confirmation.
/// </summary>
public class StagingPurchaseOrderItem
{
    public Guid Id { get; set; }
    
    public Guid StagingPurchaseOrderId { get; set; }
    public StagingPurchaseOrder StagingPurchaseOrder { get; set; } = null!;
    
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }
    
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    
    public Guid? PurchaseOrderItemId { get; set; }
    public PurchaseOrderItem? PurchaseOrderItem { get; set; }
    
    [Required]
    public string SupplierReference { get; set; } = string.Empty;
    public string? SupplierProductUrl { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Quantity { get; set; }
    
    public decimal ListedUnitPrice { get; set; }
    public decimal ActualUnitPrice { get; set; }
    
    [Required]
    public string RawData { get; set; } = string.Empty;
    public bool IsImported { get; set; }
    public CandidateStatus Status { get; set; }
}
