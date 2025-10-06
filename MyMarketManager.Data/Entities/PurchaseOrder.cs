using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Entities;

/// <summary>
/// A record of an order placed with a supplier, including costs and overhead allocations.
/// </summary>
public class PurchaseOrder
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public DateTime OrderDate { get; set; }
    public ProcessingStatus Status { get; set; }
    public decimal ShippingFees { get; set; }
    public decimal ImportFees { get; set; }
    public decimal InsuranceFees { get; set; }
    public decimal AdditionalFees { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public Supplier Supplier { get; set; } = null!;
    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
    public ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
    public ICollection<StagingPurchaseOrder> StagingPurchaseOrders { get; set; } = new List<StagingPurchaseOrder>();
}
