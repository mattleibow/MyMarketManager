using System.ComponentModel.DataAnnotations;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.Api.Models;

/// <summary>
/// Request model for updating an existing product
/// </summary>
public class UpdateProductRequest
{
    public string? SKU { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    public ProductQuality Quality { get; set; }
    public string? Notes { get; set; }
    public int StockOnHand { get; set; }
}
