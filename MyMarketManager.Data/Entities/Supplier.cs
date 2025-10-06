using System.ComponentModel.DataAnnotations;

namespace MyMarketManager.Data.Entities;

/// <summary>
/// Represents a vendor or store from which goods are purchased.
/// </summary>
public class Supplier : EntityBase
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? WebsiteUrl { get; set; }
    public string? ContactInfo { get; set; }

    // Navigation properties
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    public ICollection<StagingBatch> StagingBatches { get; set; } = new List<StagingBatch>();
}
