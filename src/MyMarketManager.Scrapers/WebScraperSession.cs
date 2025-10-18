using Microsoft.Extensions.Logging;

namespace MyMarketManager.Scrapers;

/// <summary>
/// Default implementation of IWebScraperSession that uses HttpClient to fetch pages.
/// </summary>
public class WebScraperSession(HttpClient httpClient, ILogger logger) : IWebScraperSession
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger _logger = logger;
    private bool _disposed;

    /// <inheritdoc/>
    public async Task<string> FetchPageAsync(string url, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _logger.LogDebug("Fetching from {Url}", url);

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        // Note: HttpClient is created and owned by the factory via HttpClientHandler.
        // The handler should be disposed, not the client itself, to properly release resources.
        // However, since we receive the client from the factory, we dispose it here.
        // The factory creates a new HttpClientHandler for each session, so this is safe.
        _httpClient?.Dispose();
        _disposed = true;
    }
}
