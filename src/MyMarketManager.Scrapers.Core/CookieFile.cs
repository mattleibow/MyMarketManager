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

/// <summary>
/// Represents a single HTTP cookie.
/// </summary>
public class CookieData
{
    /// <summary>
    /// Cookie name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Cookie value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Cookie domain (e.g., ".shein.com").
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// Cookie path (e.g., "/").
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Whether the cookie is secure (HTTPS only).
    /// </summary>
    public bool Secure { get; set; }

    /// <summary>
    /// Whether the cookie is HTTP-only (not accessible via JavaScript).
    /// </summary>
    public bool HttpOnly { get; set; }

    /// <summary>
    /// Optional cookie expiration timestamp.
    /// </summary>
    public DateTimeOffset? Expires { get; set; }

    /// <summary>
    /// SameSite attribute (None, Lax, Strict).
    /// </summary>
    public string? SameSite { get; set; }
}
