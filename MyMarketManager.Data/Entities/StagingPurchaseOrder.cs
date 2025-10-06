namespace MyMarketManager.Data.Entities;

/// <summary>
/// A parsed supplier order stored in staging before validation and promotion.
/// </summary>
public class StagingPurchaseOrder
{
    public int Id { get; set; }
    public int StagingBatchId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public string SupplierReference { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string RawData { get; set; } = string.Empty;
    public bool IsImported { get; set; }

    // Navigation properties
    public StagingBatch StagingBatch { get; set; } = null!;
    public PurchaseOrder? PurchaseOrder { get; set; }
    public ICollection<StagingPurchaseOrderItem> Items { get; set; } = new List<StagingPurchaseOrderItem>();
}
