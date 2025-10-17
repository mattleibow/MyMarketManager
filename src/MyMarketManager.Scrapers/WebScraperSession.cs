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

        _httpClient?.Dispose();
        _disposed = true;
    }
}
