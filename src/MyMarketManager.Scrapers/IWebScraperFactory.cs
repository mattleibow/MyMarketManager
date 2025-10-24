namespace MyMarketManager.Scrapers;

/// <summary>
/// Factory interface for creating web scraper instances.
/// </summary>
public interface IWebScraperFactory
{
    /// <summary>
    /// Creates a scraper instance for the specified scraper name.
    /// </summary>
    /// <param name="scraperName">The name of the scraper (e.g., "Shein").</param>
    /// <returns>A web scraper instance.</returns>
    WebScraper CreateScraper(string scraperName);

    /// <summary>
    /// Gets all registered scraper names.
    /// </summary>
    /// <returns>List of scraper names.</returns>
    IEnumerable<string> GetAvailableScrapers();
}
