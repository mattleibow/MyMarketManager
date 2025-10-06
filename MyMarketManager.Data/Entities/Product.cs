using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Entities;

/// <summary>
/// Represents a catalog item that can be purchased, delivered, and sold. Central to linking orders, deliveries, and sales.
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string? SKU { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProductQuality Quality { get; set; }
    public string? Notes { get; set; }
    public int StockOnHand { get; set; }

    // Navigation properties
    public ICollection<ProductPhoto> Photos { get; set; } = new List<ProductPhoto>();
    public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
    public ICollection<DeliveryItem> DeliveryItems { get; set; } = new List<DeliveryItem>();
    public ICollection<ReconciledSale> ReconciledSales { get; set; } = new List<ReconciledSale>();
    public ICollection<StagingPurchaseOrderItem> StagingPurchaseOrderItems { get; set; } = new List<StagingPurchaseOrderItem>();
    public ICollection<StagingSaleItem> StagingSaleItems { get; set; } = new List<StagingSaleItem>();
}
