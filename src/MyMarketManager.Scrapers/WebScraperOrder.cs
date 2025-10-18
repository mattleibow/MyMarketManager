namespace MyMarketManager.Scrapers;

/// <summary>
/// Represents detailed order information extracted from an order detail page.
/// Inherits from Dictionary to allow scraper-specific fields (e.g., billno, addTime, totalPrice).
/// </summary>
public class WebScraperOrder(WebScraperOrderSummary orderSummary) : Dictionary<string, string>
{
    /// <summary>
    /// Gets the order summary that led to this order detail being fetched.
    /// </summary>
    public WebScraperOrderSummary OrderSummary { get; } = orderSummary;

    /// <summary>
    /// Gets or sets the list of items in this order.
    /// </summary>
    public List<WebScraperOrderItem> OrderItems { get; set; } = new();

    /// <summary>
    /// Gets or sets the raw JSON or HTML data from which this order was extracted.
    /// </summary>
    public string? RawData { get; set; }
}
