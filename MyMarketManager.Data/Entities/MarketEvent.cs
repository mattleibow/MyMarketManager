using System.ComponentModel.DataAnnotations;

namespace MyMarketManager.Data.Entities;

/// <summary>
/// Represents a market day or event where sales occur. Used to group reconciled sales.
/// </summary>
public class MarketEvent : EntityBase
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset Date { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public ICollection<ReconciledSale> ReconciledSales { get; set; } = new List<ReconciledSale>();
}
