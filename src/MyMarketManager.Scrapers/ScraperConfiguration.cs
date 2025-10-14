namespace MyMarketManager.Scrapers;

/// <summary>
/// Configuration for web scrapers including HTTP settings and scraping behavior.
/// Can be loaded from application settings and shared across all scrapers.
/// </summary>
public class ScraperConfiguration
{
    /// <summary>
    /// User agent string to use for HTTP requests.
    /// </summary>
    public string UserAgent { get; set; } = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36";

    /// <summary>
    /// Additional HTTP headers to send with requests.
    /// </summary>
    public Dictionary<string, string> AdditionalHeaders { get; set; } = new();

    /// <summary>
    /// Delay between page requests to avoid rate limiting.
    /// </summary>
    public TimeSpan RequestDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Maximum number of concurrent requests.
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 1;

    /// <summary>
    /// Timeout for HTTP requests.
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
