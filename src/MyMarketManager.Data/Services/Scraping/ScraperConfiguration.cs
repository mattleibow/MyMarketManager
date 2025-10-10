namespace MyMarketManager.Data.Services.Scraping;

/// <summary>
/// Configuration for a web scraper including URLs, selectors, and scraping behavior.
/// </summary>
public class ScraperConfiguration
{
    /// <summary>
    /// The name of the supplier this scraper is for.
    /// </summary>
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>
    /// The base domain for the supplier's website (e.g., "shein.com").
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// The URL pattern for the orders list page.
    /// May contain placeholders like {page} for pagination.
    /// </summary>
    public string OrdersListUrl { get; set; } = string.Empty;

    /// <summary>
    /// The URL pattern for order detail pages.
    /// May contain placeholders like {orderId}.
    /// </summary>
    public string OrderDetailUrlPattern { get; set; } = string.Empty;

    /// <summary>
    /// The URL pattern for product pages.
    /// May contain placeholders like {productId} or {sku}.
    /// </summary>
    public string ProductPageUrlPattern { get; set; } = string.Empty;

    /// <summary>
    /// The URL for the account/profile page (used for cookie validation).
    /// </summary>
    public string AccountPageUrl { get; set; } = string.Empty;

    /// <summary>
    /// CSS selector or XPath to find order links on the orders list page.
    /// </summary>
    public string OrderLinkSelector { get; set; } = string.Empty;

    /// <summary>
    /// CSS selector or XPath for the order ID on the detail page.
    /// </summary>
    public string OrderIdSelector { get; set; } = string.Empty;

    /// <summary>
    /// CSS selector or XPath for the order date on the detail page.
    /// </summary>
    public string OrderDateSelector { get; set; } = string.Empty;

    /// <summary>
    /// CSS selector or XPath for order items on the detail page.
    /// </summary>
    public string OrderItemsSelector { get; set; } = string.Empty;

    /// <summary>
    /// CSS selector or XPath to detect if the user is logged in (for validation).
    /// </summary>
    public string LoggedInIndicatorSelector { get; set; } = string.Empty;

    /// <summary>
    /// User agent string to use for HTTP requests.
    /// </summary>
    public string UserAgent { get; set; } = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36";

    /// <summary>
    /// Additional HTTP headers to send with requests.
    /// </summary>
    public Dictionary<string, string> AdditionalHeaders { get; set; } = new();

    /// <summary>
    /// Delay in milliseconds between page requests to avoid rate limiting.
    /// </summary>
    public int RequestDelayMs { get; set; } = 1000;

    /// <summary>
    /// Maximum number of concurrent requests.
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 1;

    /// <summary>
    /// Timeout for HTTP requests in seconds.
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to use a headless browser (e.g., Playwright, Selenium) instead of HttpClient.
    /// Required for sites with heavy JavaScript rendering.
    /// </summary>
    public bool RequiresHeadlessBrowser { get; set; } = false;

    /// <summary>
    /// Custom scraping hints or notes for manual review.
    /// </summary>
    public string? Notes { get; set; }
}
