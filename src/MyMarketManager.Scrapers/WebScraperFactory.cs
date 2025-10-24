using Microsoft.Extensions.DependencyInjection;

namespace MyMarketManager.Scrapers;

/// <summary>
/// Factory for creating web scraper instances based on scraper name.
/// Uses dependency injection to resolve scraper instances.
/// </summary>
public class WebScraperFactory : IWebScraperFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Type> _scraperRegistry;

    public WebScraperFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _scraperRegistry = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        
        // Register known scrapers
        RegisterScrapers();
    }

    /// <summary>
    /// Registers all known scraper types.
    /// </summary>
    private void RegisterScrapers()
    {
        // Add scraper registrations here
        _scraperRegistry["Shein"] = typeof(Shein.SheinWebScraper);
        // Future scrapers can be added here:
        // _scraperRegistry["AnotherSupplier"] = typeof(AnotherSupplier.AnotherWebScraper);
    }

    /// <summary>
    /// Creates a scraper instance for the specified scraper name.
    /// </summary>
    public WebScraper CreateScraper(string scraperName)
    {
        if (!_scraperRegistry.TryGetValue(scraperName, out var scraperType))
        {
            throw new ArgumentException($"Unknown scraper: {scraperName}", nameof(scraperName));
        }

        var scraper = _serviceProvider.GetRequiredService(scraperType) as WebScraper;
        if (scraper == null)
        {
            throw new InvalidOperationException($"Failed to create scraper instance for {scraperName}");
        }

        return scraper;
    }

    /// <summary>
    /// Gets all registered scraper names.
    /// </summary>
    public IEnumerable<string> GetAvailableScrapers()
    {
        return _scraperRegistry.Keys.OrderBy(k => k);
    }
}
