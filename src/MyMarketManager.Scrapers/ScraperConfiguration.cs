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
    public string UserAgent { get; set; } = string.Empty;

    /// <summary>
    /// Additional HTTP headers to send with requests.
    /// </summary>
    public Dictionary<string, string> AdditionalHeaders { get; set; } = new();

    /// <summary>
    /// Delay between page requests to avoid rate limiting.
    /// </summary>
    public TimeSpan RequestDelay { get; set; }

    /// <summary>
    /// Maximum number of concurrent requests.
    /// </summary>
    public int MaxConcurrentRequests { get; set; }

    /// <summary>
    /// Timeout for HTTP requests.
    /// </summary>
    public TimeSpan RequestTimeout { get; set; }

    /// <summary>
    /// Gets a new instance of ScraperConfiguration with default values.
    /// </summary>
    public static ScraperConfiguration Defaults =>
        new()
        {
            UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36",
            AdditionalHeaders = new Dictionary<string, string>
            {
                ["Accept"] = "text/html",
                ["Accept-Language"] = "en-US",
                ["Cache-Control"] = "no-cache",
                ["Upgrade-Insecure-Requests"] = "1"
            },
            RequestDelay = TimeSpan.FromSeconds(2),
            MaxConcurrentRequests = 1,
            RequestTimeout = TimeSpan.FromSeconds(30)
        };
}
