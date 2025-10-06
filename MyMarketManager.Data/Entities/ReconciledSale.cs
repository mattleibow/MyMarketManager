namespace MyMarketManager.Data.Entities;

/// <summary>
/// A confirmed sale linked to a product and market event, derived from imported records and stocktake.
/// </summary>
public class ReconciledSale
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int MarketEventId { get; set; }
    public int Quantity { get; set; }
    public decimal SalePrice { get; set; }

    // Navigation properties
    public Product Product { get; set; } = null!;
    public MarketEvent MarketEvent { get; set; } = null!;
}
