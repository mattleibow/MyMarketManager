using System.ComponentModel.DataAnnotations;

namespace MyMarketManager.Data.Entities;

/// <summary>
/// Stores one or more images associated with a product.
/// </summary>
public class ProductPhoto : EntityBase
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Required]
    public string Url { get; set; } = string.Empty;
    public string? Caption { get; set; }
}
