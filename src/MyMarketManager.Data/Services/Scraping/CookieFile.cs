using System.Text.Json.Serialization;

namespace MyMarketManager.Data.Services.Scraping;

/// <summary>
/// Represents a cookie file format for web scraping sessions.
/// This file can be POSTed to the server and stored for use in scraping operations.
/// </summary>
public class CookieFile
{
    /// <summary>
    /// Unique identifier for this cookie file.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Supplier ID associated with these cookies (e.g., Shein supplier).
    /// </summary>
    public Guid SupplierId { get; set; }

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
    /// The list of cookies captured from the browser session.
    /// </summary>
    public List<CookieData> Cookies { get; set; } = new();

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
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Cookie value.
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Cookie domain (e.g., ".shein.com").
    /// </summary>
    [JsonPropertyName("domain")]
    public string? Domain { get; set; }

    /// <summary>
    /// Cookie path (e.g., "/").
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>
    /// Whether the cookie is secure (HTTPS only).
    /// </summary>
    [JsonPropertyName("secure")]
    public bool Secure { get; set; }

    /// <summary>
    /// Whether the cookie is HTTP-only (not accessible via JavaScript).
    /// </summary>
    [JsonPropertyName("httpOnly")]
    public bool HttpOnly { get; set; }

    /// <summary>
    /// Optional cookie expiration timestamp.
    /// </summary>
    [JsonPropertyName("expires")]
    public DateTimeOffset? Expires { get; set; }

    /// <summary>
    /// SameSite attribute (None, Lax, Strict).
    /// </summary>
    [JsonPropertyName("sameSite")]
    public string? SameSite { get; set; }
}
