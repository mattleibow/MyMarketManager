using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Entities;

/// <summary>
/// A record of an order placed with a supplier, including costs and overhead allocations.
/// </summary>
public class PurchaseOrder
{
    public Guid Id { get; set; }
    
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    
    public DateTimeOffset OrderDate { get; set; }
    public ProcessingStatus Status { get; set; }
    
    public decimal ShippingFees { get; set; }
    public decimal ImportFees { get; set; }
    public decimal InsuranceFees { get; set; }
    public decimal AdditionalFees { get; set; }
    
    public string? Notes { get; set; }

    // Navigation properties
    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
    public ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
    public ICollection<StagingPurchaseOrder> StagingPurchaseOrders { get; set; } = new List<StagingPurchaseOrder>();
}
