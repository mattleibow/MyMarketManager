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
                var cookieDomain = cookie.Domain ?? cookies.Domain;
                var uriDomain = cookieDomain.TrimStart('.');
                var uri = new Uri($"https://{uriDomain}");
                var systemCookie = new Cookie
                {
                    Name = cookie.Name,
                    Value = cookie.Value,
                    Domain = cookie.Domain ?? $".{cookies.Domain}",
                    Path = cookie.Path ?? "/",
                    Secure = cookie.Secure,
                    HttpOnly = cookie.HttpOnly
                };
                handler.CookieContainer.Add(uri, systemCookie);

                _logger.LogDebug("Added cookie {CookieName} for domain {Domain}", cookie.Name, cookieDomain);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to add cookie {CookieName} with domain {Domain}", cookie.Name, cookie.Domain);
            }
        }

        // Create the actual HttpClient
        var client = new HttpClient(handler);

        // Set timeouts
        client.Timeout = _configuration.RequestTimeout;

        // Add headers
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", _configuration.UserAgent);

        // Add other headers in order from config
        foreach (var header in _configuration.AdditionalHeaders)
        {
            try
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                _logger.LogDebug("Added header {HeaderName}: {HeaderValue}", header.Key, header.Value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to add header {HeaderName}", header.Key);
            }
        }

        return client;
    }
}
