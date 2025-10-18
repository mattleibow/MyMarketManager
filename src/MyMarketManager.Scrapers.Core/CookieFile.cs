namespace MyMarketManager.Scrapers.Core;

/// <summary>
/// Represents a cookie file format for web scraping sessions.
/// This file can be POSTed to the server and stored for use in scraping operations.
/// </summary>
public class CookieFile
{
    /// <summary>
    /// The base domain these cookies are for (e.g., "shein.com").
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when these cookies were captured.
    /// </summary>
    public DateTimeOffset CapturedAt { get; set; }

    /// <summary>
    /// Optional expiration timestamp. After this time, cookies should be re-captured.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Dictionary of cookies captured from the browser session, keyed by cookie name.
    /// </summary>
    public Dictionary<string, CookieData> Cookies { get; set; } = new();

    /// <summary>
    /// Optional metadata about the capture session.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}
