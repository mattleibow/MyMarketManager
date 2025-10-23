using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMarketManager.Scrapers.Core;

namespace MyMarketManager.Scrapers;

/// <summary>
/// Default factory implementation that creates HTTP-based scraping sessions.
/// </summary>
public class WebScraperSessionFactory(
    ILogger<WebScraperSessionFactory> logger,
    IOptions<ScraperConfiguration> configuration) : IWebScraperSessionFactory
{
    private readonly ILogger<WebScraperSessionFactory> _logger = logger;
    private readonly ScraperConfiguration _configuration = configuration.Value;

    /// <inheritdoc/>
    public IWebScraperSession CreateSession(CookieFile cookies)
    {
        var httpClient = CreateHttpClient(cookies);
        return new WebScraperSession(httpClient, _logger);
    }

    /// <summary>
    /// Creates an HttpClient configured with cookies and headers for scraping.
    /// </summary>
    protected virtual HttpClient CreateHttpClient(CookieFile cookies)
    {
        var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new CookieContainer()
        };

        // Add cookies to the container
        foreach (var cookie in cookies.Cookies.Values)
        {
            try
            {
                handler.CookieContainer.Add(new Uri($"https://{cookies.Domain}"), new Cookie
                {
                    Name = cookie.Name,
                    Value = cookie.Value,
                    Domain = cookie.Domain ?? $".{cookies.Domain}",
                    Path = cookie.Path ?? "/",
                    Secure = cookie.Secure,
                    HttpOnly = cookie.HttpOnly
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to add cookie {CookieName}", cookie.Name);
            }
        }

        // Create the actual HttpClient
        var client = new HttpClient(handler);

        // Set timeouts
        client.Timeout = _configuration.RequestTimeout;

        // Add headers
        client.DefaultRequestHeaders.Add("user-agent", _configuration.UserAgent);
        foreach (var header in _configuration.AdditionalHeaders)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
        }

        return client;
    }
}
