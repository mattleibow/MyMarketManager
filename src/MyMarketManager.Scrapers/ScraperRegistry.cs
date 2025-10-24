namespace MyMarketManager.Scrapers;

/// <summary>
/// Registry that maintains a list of available web scrapers.
/// </summary>
public class ScraperRegistry : IScraperRegistry
{
    private readonly List<string> _scraperNames = new();

    public ScraperRegistry()
    {
        // Register scraper names
        _scraperNames.Add("Shein");
        // Future scrapers can be added here:
        // _scraperNames.Add("AnotherSupplier");
    }

    public IEnumerable<string> GetAvailableScrapers()
    {
        return _scraperNames;
    }
}
