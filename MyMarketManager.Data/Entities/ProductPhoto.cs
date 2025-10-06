namespace MyMarketManager.Data.Entities;

/// <summary>
/// Stores one or more images associated with a product.
/// </summary>
public class ProductPhoto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Caption { get; set; }

    // Navigation properties
    public Product Product { get; set; } = null!;
}
