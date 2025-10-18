using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace MyMarketManager.Scrapers;

/// <summary>
/// Default implementation of IWebScraperSession that uses HttpClient to fetch pages.
/// </summary>
public class WebScraperSession : IWebScraperSession
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebScraperSession"/> class.
    /// </summary>
    /// <param name="handler">The HTTP handler with cookies and configuration.</param>
    /// <param name="timeout">The request timeout.</param>
    /// <param name="userAgent">The user agent string.</param>
    /// <param name="additionalHeaders">Additional HTTP headers.</param>
    /// <param name="logger">The logger instance.</param>
    public WebScraperSession(
        HttpMessageHandler handler, 
        TimeSpan timeout,
        string userAgent,
        Dictionary<string, string> additionalHeaders,
        ILogger logger)
    {
        _logger = logger;
        
        // Session owns the HttpClient and handler lifecycle
        _httpClient = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = timeout
        };

        // Add headers
        _httpClient.DefaultRequestHeaders.Add("user-agent", userAgent);
        foreach (var header in additionalHeaders)
        {
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
        }
    }

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

        // Dispose HttpClient, which also disposes the handler
        _httpClient?.Dispose();
        _disposed = true;
    }
}
