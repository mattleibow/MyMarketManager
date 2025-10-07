using MyMarketManager.Data.Enums;

namespace MyMarketManager.Api.Models;

/// <summary>
/// Data transfer object for product information
/// </summary>
public class ProductDto
{
    public Guid Id { get; set; }
    public string? SKU { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProductQuality Quality { get; set; }
    public string? Notes { get; set; }
    public int StockOnHand { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
