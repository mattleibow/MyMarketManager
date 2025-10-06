using System.ComponentModel.DataAnnotations;

namespace MyMarketManager.Data.Entities;

/// <summary>
/// A parsed sale stored in staging before validation and promotion.
/// </summary>
public class StagingSale : EntityBase
{
    public Guid StagingBatchId { get; set; }
    public StagingBatch StagingBatch { get; set; } = null!;
    
    public DateTimeOffset SaleDate { get; set; }
    
    [Required]
    public string RawData { get; set; } = string.Empty;
    public bool IsImported { get; set; }

    // Navigation properties
    public ICollection<StagingSaleItem> Items { get; set; } = new List<StagingSaleItem>();
}
