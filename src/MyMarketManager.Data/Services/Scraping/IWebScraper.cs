namespace MyMarketManager.Data.Services.Scraping;

/// <summary>
/// Defines the contract for web scrapers that extract data from supplier websites.
/// </summary>
public interface IWebScraper
{
    /// <summary>
    /// Gets the configuration for this scraper.
    /// </summary>
    ScraperConfiguration Configuration { get; }

    /// <summary>
    /// Scrapes orders from the supplier's website and creates a staging batch.
    /// </summary>
    /// <param name="cookieFile">The cookie file containing authentication cookies.</param>
    /// <param name="lastSuccessfulScrape">The timestamp of the last successful scrape, if any.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the created staging batch.</returns>
    Task<Guid> ScrapeOrdersAsync(CookieFile cookieFile, DateTimeOffset? lastSuccessfulScrape, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the cookies are still valid by attempting to load a protected page.
    /// </summary>
    /// <param name="cookieFile">The cookie file to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if cookies are valid, false otherwise.</returns>
    Task<bool> ValidateCookiesAsync(CookieFile cookieFile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scrapes a specific page type and returns the raw HTML.
    /// </summary>
    /// <param name="pageType">The type of page to scrape.</param>
    /// <param name="cookieFile">The cookie file containing authentication cookies.</param>
    /// <param name="pageUrl">Optional specific URL to scrape. If null, uses the default URL from configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The raw HTML content of the page.</returns>
    Task<string> ScrapePageAsync(PageType pageType, CookieFile cookieFile, string? pageUrl = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts order links from the orders list page.
    /// </summary>
    /// <param name="ordersListHtml">The HTML content of the orders list page.</param>
    /// <returns>A list of order URLs to scrape.</returns>
    IEnumerable<string> ExtractOrderLinks(string ordersListHtml);

    /// <summary>
    /// Parses order details from an order detail page.
    /// </summary>
    /// <param name="orderDetailHtml">The HTML content of the order detail page.</param>
    /// <returns>The parsed order data as a dictionary.</returns>
    Task<Dictionary<string, object>> ParseOrderDetailsAsync(string orderDetailHtml, CancellationToken cancellationToken = default);
}

/// <summary>
/// Types of pages that can be scraped.
/// </summary>
public enum PageType
{
    /// <summary>
    /// Product detail page showing information about a single product.
    /// </summary>
    ProductPage,

    /// <summary>
    /// Orders list page showing a list of orders.
    /// </summary>
    OrdersListPage,

    /// <summary>
    /// Order details page showing details of a specific order.
    /// </summary>
    OrderDetailsPage,

    /// <summary>
    /// Account/profile page for validation.
    /// </summary>
    AccountPage
}
