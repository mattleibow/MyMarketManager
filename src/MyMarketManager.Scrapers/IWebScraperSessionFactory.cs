using MyMarketManager.Scrapers.Core;

namespace MyMarketManager.Scrapers;

/// <summary>
/// Factory for creating web scraper sessions configured with cookies and HTTP settings.
/// </summary>
public interface IWebScraperSessionFactory
{
    /// <summary>
    /// Creates a new scraping session with the provided cookies.
    /// </summary>
    /// <param name="cookies">The cookie file containing authentication cookies.</param>
    /// <returns>A new scraping session.</returns>
    IWebScraperSession CreateSession(CookieFile cookies);
}
