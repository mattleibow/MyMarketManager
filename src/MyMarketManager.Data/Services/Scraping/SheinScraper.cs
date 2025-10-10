using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMarketManager.Data.Entities;

namespace MyMarketManager.Data.Services.Scraping;

/// <summary>
/// Web scraper implementation for Shein.com orders.
/// </summary>
public class SheinScraper : WebScraperBase
{
    public SheinScraper(
        MyMarketManagerDbContext context,
        ILogger<SheinScraper> logger,
        IOptions<ScraperConfiguration> configuration)
        : base(context, logger, configuration)
    {
    }

    /// <summary>
    /// Validates a page's HTML content by checking for gbRawData which indicates successful authentication.
    /// </summary>
    protected override Task<bool> ValidatePageAsync(string html, CancellationToken cancellationToken)
    {
        // Check for gbRawData which indicates successful authentication on Shein pages
        var isValid = html.Contains("gbRawData");
        return Task.FromResult(isValid);
    }

    /// <summary>
    /// Extracts order links from the orders list page and returns template values for each order.
    /// </summary>
    protected override IEnumerable<Dictionary<string, string>> ExtractOrderLinks(string ordersListHtml)
    {
        Logger.LogDebug("Extracting order links from HTML (length: {Length})", ordersListHtml.Length);

        var links = new List<Dictionary<string, string>>();

        // Simple regex-based extraction (to be replaced with proper HTML parsing)
        var pattern = @"href=""(/user/orders/detail\?order_id=([^""&]+))""";
        var matches = Regex.Matches(ordersListHtml, pattern);

        foreach (Match match in matches)
        {
            if (match.Groups.Count > 2)
            {
                var orderId = match.Groups[2].Value;
                var linkInfo = new Dictionary<string, string>
                {
                    { "orderId", orderId }
                };
                
                // Check for duplicates
                if (!links.Any(l => l.ContainsKey("orderId") && l["orderId"] == orderId))
                {
                    links.Add(linkInfo);
                }
            }
        }

        Logger.LogInformation("Extracted {Count} unique order links", links.Count);
        return links;
    }

    /// <summary>
    /// Parses order details from an order detail page.
    /// </summary>
    protected override Task<Dictionary<string, object>> ParseOrderDetailsAsync(string orderDetailHtml, CancellationToken cancellationToken)
    {
        Logger.LogDebug("Parsing order details from HTML (length: {Length})", orderDetailHtml.Length);

        // Extract gbRawData from the page
        var gbRawData = ExtractGbRawData(orderDetailHtml);

        var orderData = new Dictionary<string, object>
        {
            { "html_length", orderDetailHtml.Length },
            { "scraped_at", DateTimeOffset.UtcNow.ToString("o") },
            { "has_gbRawData", gbRawData != null }
        };

        // Store the actual gbRawData if found
        if (gbRawData != null)
        {
            orderData["raw_data"] = gbRawData;
        }

        return Task.FromResult(orderData);
    }

    /// <summary>
    /// Extracts gbRawData JSON object from the HTML page.
    /// </summary>
    private string? ExtractGbRawData(string html)
    {
        try
        {
            // Look for gbRawData in the HTML
            var pattern = @"gbRawData\s*=\s*(\{.*?\});";
            var match = Regex.Match(html, pattern, RegexOptions.Singleline);

            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }

            Logger.LogWarning("Could not extract gbRawData from HTML");
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error extracting gbRawData");
            return null;
        }
    }
}
