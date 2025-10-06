using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Entities;

/// <summary>
/// Represents a shipment or receipt of goods, which may be linked to a purchase order or stand alone.
/// </summary>
public class Delivery
{
    public Guid Id { get; set; }
    
    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    
    public DateTimeOffset DeliveryDate { get; set; }
    public string? Courier { get; set; }
    public string? TrackingNumber { get; set; }
    public ProcessingStatus Status { get; set; }

    // Navigation properties
    public ICollection<DeliveryItem> Items { get; set; } = new List<DeliveryItem>();
}
