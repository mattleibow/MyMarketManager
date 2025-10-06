using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Entities;

/// <summary>
/// Individual items received in a delivery, with quality and inspection details.
/// </summary>
public class DeliveryItem
{
    public int Id { get; set; }
    public int DeliveryId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public ProductQuality Quality { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public Delivery Delivery { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
