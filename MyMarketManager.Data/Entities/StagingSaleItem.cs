using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Entities;

/// <summary>
/// Raw sales data imported from third‑party reports before reconciliation.
/// </summary>
public class StagingSaleItem
{
    public int Id { get; set; }
    public int StagingSaleId { get; set; }
    public string ProductDescription { get; set; } = string.Empty;
    public int? ProductId { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string? MarketEventName { get; set; }
    public string RawData { get; set; } = string.Empty;
    public bool IsImported { get; set; }
    public CandidateStatus Status { get; set; }

    // Navigation properties
    public StagingSale StagingSale { get; set; } = null!;
    public Product? Product { get; set; }
}
