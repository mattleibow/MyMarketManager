using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMarketManager.Data.Entities;

namespace MyMarketManager.Data.Services.Scraping;

/// <summary>
/// Web scraper implementation for Shein.com orders.
/// </summary>
public class SheinScraper(
        MyMarketManagerDbContext context,
        ILogger<SheinScraper> logger,
        IOptions<ScraperConfiguration> configuration)
        : WebScraperBase(context, logger, configuration)
{
    private const string Domain = "shein.com";
    private const string AccountPageUrl = "https://shein.com/user/account";
    private const string OrdersListUrl = "https://shein.com/user/orders/list";
    private const string OrderDetailUrlTemplate = "https://shein.com/user/orders/detail?order_id={orderId}";
    private const string ProductPageUrlTemplate = "https://shein.com/product/{productId}";

    /// <inheritdoc/>
    protected override string GetAccountPageUrl() => AccountPageUrl;

    /// <inheritdoc/>
    protected override string GetOrdersListUrl() => OrdersListUrl;

    /// <inheritdoc/>
    protected override string GetOrderDetailUrl(WebScraperOrderSummary order) => ReplaceUrlTemplateValues(OrderDetailUrlTemplate, order);

    /// <inheritdoc/>
    protected override Task<bool> ValidatePageAsync(string html, CancellationToken cancellationToken)
    {
        // Check for gbRawData which indicates successful authentication on Shein pages
        var isValid = html.Contains("gbRawData");
        return Task.FromResult(isValid);
    }

    /// <inheritdoc/>
    protected override IEnumerable<WebScraperOrderSummary> ParseOrdersListAsync(string ordersListHtml)
    {
        Logger.LogDebug("Parsing orders from HTML (length: {Length})", ordersListHtml.Length);

        var links = new List<WebScraperOrderSummary>();

        // Simple regex-based extraction (to be replaced with proper HTML parsing)
        var pattern = @"href=""(/user/orders/detail\?order_id=([^""&]+))""";
        var matches = Regex.Matches(ordersListHtml, pattern);

        foreach (Match match in matches)
        {
            if (match.Groups.Count > 2)
            {
                var orderId = match.Groups[2].Value;
                var linkInfo = new WebScraperOrderSummary
                {
                    RawData = "",
                    ["orderId"] = orderId
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

    /// <inheritdoc/>
    protected override Task<WebScraperOrder> ParseOrderDetailsAsync(string orderDetailHtml, WebScraperOrderSummary orderSummary, CancellationToken cancellationToken)
    {
        Logger.LogDebug("Parsing order details from HTML (length: {Length})", orderDetailHtml.Length);

        // Extract gbRawData from the page
        var gbRawData = ExtractGbRawData(orderDetailHtml);

        var orderData = new WebScraperOrder(orderSummary)
        {
            RawData = gbRawData ?? string.Empty,
        };

        return Task.FromResult(orderData);
    }

    /// <inheritdoc/>
    protected override Task UpdateStagingOrderAsync(StagingPurchaseOrder stagingOrder, WebScraperOrder order, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
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
