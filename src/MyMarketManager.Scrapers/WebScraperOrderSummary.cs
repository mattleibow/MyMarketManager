namespace MyMarketManager.Scrapers;

/// <summary>
/// Represents summary information about an order extracted from an orders list page.
/// Inherits from Dictionary to allow scraper-specific fields (e.g., orderId, orderNumber).
/// </summary>
public class WebScraperOrderSummary : Dictionary<string, string>
{
    /// <summary>
    /// Gets or sets the raw JSON or text data from which this summary was extracted.
    /// </summary>
    public string? RawData { get; set; }
}
