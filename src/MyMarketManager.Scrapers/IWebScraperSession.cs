namespace MyMarketManager.Scrapers;

/// <summary>
/// Represents a web scraping session with configured HTTP client for fetching pages.
/// Sessions should be disposed after use to release HTTP resources.
/// </summary>
public interface IWebScraperSession : IDisposable
{
    /// <summary>
    /// Fetches HTML content from the specified URL.
    /// </summary>
    /// <param name="url">The URL to fetch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The HTML content as a string.</returns>
    Task<string> FetchPageAsync(string url, CancellationToken cancellationToken = default);
}
