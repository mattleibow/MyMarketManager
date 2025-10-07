using System.ComponentModel.DataAnnotations;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Entities;

/// <summary>
/// Raw sales data imported from third‑party reports before reconciliation.
/// </summary>
public class StagingSaleItem : EntityBase
{
    public Guid StagingSaleId { get; set; }
    public StagingSale StagingSale { get; set; } = null!;

    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }

    [Required]
    public string ProductDescription { get; set; } = string.Empty;
    public DateTimeOffset SaleDate { get; set; }

    public decimal Price { get; set; }

    public int Quantity { get; set; }
    public string? MarketEventName { get; set; }

    [Required]
    public string RawData { get; set; } = string.Empty;
    public bool IsImported { get; set; }
    public CandidateStatus Status { get; set; }
}
