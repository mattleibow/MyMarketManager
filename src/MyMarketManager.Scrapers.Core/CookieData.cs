namespace MyMarketManager.Scrapers.Core;

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
