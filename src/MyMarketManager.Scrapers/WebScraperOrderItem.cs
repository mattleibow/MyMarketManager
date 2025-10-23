namespace MyMarketManager.Scrapers;

/// <summary>
/// Represents a single item within an order.
/// Inherits from Dictionary to allow scraper-specific fields (e.g., goods_id, goods_name, goods_qty).
/// </summary>
public class WebScraperOrderItem(WebScraperOrder order) : Dictionary<string, string>
{
    /// <summary>
    /// Gets the parent order that contains this item.
    /// </summary>
    public WebScraperOrder Order { get; } = order;

    /// <summary>
    /// Gets or sets the raw JSON data from which this item was extracted.
    /// </summary>
    public string? RawData { get; set; }
}
