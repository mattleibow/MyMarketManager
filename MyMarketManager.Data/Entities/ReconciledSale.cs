namespace MyMarketManager.Data.Entities;

/// <summary>
/// A confirmed sale linked to a product and market event, derived from imported records and stocktake.
/// </summary>
public class ReconciledSale
{
    public Guid Id { get; set; }
    
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    
    public Guid MarketEventId { get; set; }
    public MarketEvent MarketEvent { get; set; } = null!;
    
    public int Quantity { get; set; }
    public decimal SalePrice { get; set; }
}
