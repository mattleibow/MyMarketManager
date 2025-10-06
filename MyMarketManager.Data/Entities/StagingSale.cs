namespace MyMarketManager.Data.Entities;

/// <summary>
/// A parsed sale stored in staging before validation and promotion.
/// </summary>
public class StagingSale
{
    public int Id { get; set; }
    public int StagingBatchId { get; set; }
    public DateTime SaleDate { get; set; }
    public string RawData { get; set; } = string.Empty;
    public bool IsImported { get; set; }

    // Navigation properties
    public StagingBatch StagingBatch { get; set; } = null!;
    public ICollection<StagingSaleItem> Items { get; set; } = new List<StagingSaleItem>();
}
