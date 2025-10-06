using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Entities;

/// <summary>
/// Individual items received in a delivery, with quality and inspection details.
/// </summary>
public class DeliveryItem
{
    public Guid Id { get; set; }
    
    public Guid DeliveryId { get; set; }
    public Delivery Delivery { get; set; } = null!;
    
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    
    public int Quantity { get; set; }
    public ProductQuality Quality { get; set; }
    public string? Notes { get; set; }
}
