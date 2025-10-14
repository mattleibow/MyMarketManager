using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMarketManager.Data;
using MyMarketManager.Data.Entities;

namespace MyMarketManager.Scrapers;

/// <summary>
/// Web scraper implementation for Shein.com orders.
/// </summary>
public class SheinWebScraper(
    MyMarketManagerDbContext context,
    ILogger<SheinWebScraper> logger,
    IOptions<ScraperConfiguration> configuration)
    : WebScraper(context, logger, configuration)
{
    private const string OrdersListUrl = "https://shein.com/user/orders/list";
    private const string OrderDetailUrlTemplate = "https://shein.com/user/orders/detail/{orderId}";

    /// <inheritdoc/>
    public override string GetOrdersListUrl() => OrdersListUrl;

    /// <inheritdoc/>
    public override string GetOrderDetailUrl(WebScraperOrderSummary order) => ReplaceUrlTemplateValues(OrderDetailUrlTemplate, order);

    /// <inheritdoc/>
    public override async IAsyncEnumerable<WebScraperOrderSummary> ParseOrdersListAsync(string ordersListHtml, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Logger.LogDebug("Parsing orders from HTML (length: {Length})", ordersListHtml.Length);

        var gbRawData = ExtractGbRawData(ordersListHtml);
        var json = JsonDocument.Parse(gbRawData ?? "{}");

        var orders = json.RootElement.GetProperty("order_list").EnumerateArray().ToList();
        var orderNumbers = orders.Select(o => o.GetProperty("billno").GetString()).ToList();

        // Simple regex-based extraction (to be replaced with proper HTML parsing)
        var pattern = @"href=""(/user/orders/detail\?order_id=([^""&]+))""";
        var matches = Regex.Matches(ordersListHtml, pattern);

        var count = 0;

        foreach (Match match in matches)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (match.Groups.Count > 2)
            {
                var orderId = match.Groups[2].Value;
                var linkInfo = new WebScraperOrderSummary
                {
                    RawData = "",
                    ["orderId"] = orderId
                };

                count++;

                yield return linkInfo;
            }
        }

        Logger.LogInformation("Parsed {Count} unique orders", count);
    }

    /// <inheritdoc/>
    public override Task<WebScraperOrder> ParseOrderDetailsAsync(string orderDetailHtml, WebScraperOrderSummary orderSummary, CancellationToken cancellationToken)
    {
        Logger.LogDebug("Parsing order details from HTML (length: {Length})", orderDetailHtml.Length);

        // Extract gbRawData from the page
        var gbRawData = ExtractGbRawData(orderDetailHtml);

        var orderData = new WebScraperOrder(orderSummary)
        {
            RawData = gbRawData,
        };

        return Task.FromResult(orderData);
    }

    /// <inheritdoc/>
    public override Task UpdateStagingPurchaseOrderAsync(StagingPurchaseOrder stagingOrder, WebScraperOrder order, CancellationToken cancellationToken)
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
            var pattern = @"gbRawData\s*=\s*(\{.*\})";
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
