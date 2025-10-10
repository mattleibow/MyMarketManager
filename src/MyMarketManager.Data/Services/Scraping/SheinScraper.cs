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
    private const string Domain = "shein.com";
    private const string AccountPageUrl = "https://shein.com/user/account";
    private const string OrdersListUrl = "https://shein.com/user/orders/list";
    private const string OrderDetailUrlTemplate = "https://shein.com/user/orders/detail?order_id={orderId}";
    private const string ProductPageUrlTemplate = "https://shein.com/product/{productId}";

    public SheinScraper(
        MyMarketManagerDbContext context,
        ILogger<SheinScraper> logger,
        IOptions<ScraperConfiguration> configuration)
        : base(context, logger, configuration)
    {
    }

    /// <inheritdoc/>
    protected override string GetAccountPageUrl() => AccountPageUrl;

    /// <inheritdoc/>
    protected override string GetOrdersListUrl() => OrdersListUrl;

    /// <inheritdoc/>
    protected override string GetOrderDetailUrl(Dictionary<string, string> orderLinkInfo)
    {
        return ReplaceUrlTemplateValues(OrderDetailUrlTemplate, orderLinkInfo);
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
    /// Parses the orders list page and extracts order information.
    /// </summary>
    protected override IEnumerable<Dictionary<string, string>> ParseOrdersListAsync(string ordersListHtml)
    {
        Logger.LogDebug("Parsing orders from HTML (length: {Length})", ordersListHtml.Length);

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

        Logger.LogInformation("Parsed {Count} unique orders", links.Count);
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
