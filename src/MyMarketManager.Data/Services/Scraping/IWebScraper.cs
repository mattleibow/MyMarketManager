namespace MyMarketManager.Data.Services.Scraping;

/// <summary>
/// Defines the contract for web scrapers that extract data from supplier websites.
/// </summary>
public interface IWebScraper
{
    /// <summary>
    /// Scrapes orders from the supplier's website and creates a staging batch.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ScrapeAsync(CancellationToken cancellationToken = default);
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
