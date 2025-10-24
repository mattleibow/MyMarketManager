namespace MyMarketManager.Scrapers;

/// <summary>
/// Service that provides information about available web scrapers.
/// </summary>
public interface IScraperRegistry
{
    /// <summary>
    /// Gets the names of all registered web scrapers.
    /// </summary>
    IEnumerable<string> GetAvailableScrapers();
}
