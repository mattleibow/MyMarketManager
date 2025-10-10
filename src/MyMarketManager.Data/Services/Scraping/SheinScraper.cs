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

    /// <inheritdoc/>
    protected override async Task ExecuteScrapingAsync(StagingBatch batch, CancellationToken cancellationToken)
    {
        // Step 1: Scrape orders list page
        Logger.LogInformation("Fetching orders list page");
        var ordersListHtml = await ScrapePageAsync(
            PageType.OrdersListPage,
            Configuration.OrdersListUrlTemplate,
            cancellationToken);

        // Extract gbRawData from orders list page if available
        var ordersListRawData = ExtractGbRawData(ordersListHtml);

        // Step 2: Extract order links
        Logger.LogInformation("Extracting order links from orders list");
        var orderLinks = ExtractOrderLinks(ordersListHtml).ToList();
        Logger.LogInformation("Found {Count} order links", orderLinks.Count);

        // Step 3: Scrape each order detail page
        foreach (var orderLink in orderLinks)
        {
            try
            {
                await Task.Delay(Configuration.RequestDelay, cancellationToken);

                Logger.LogInformation("Scraping order: {OrderLink}", orderLink);
                var orderHtml = await ScrapePageAsync(PageType.OrderDetailsPage, orderLink, cancellationToken);

                // Parse order details
                var orderData = await ParseOrderDetailsAsync(orderHtml, cancellationToken);

                // Extract gbRawData from order detail page
                var orderRawData = ExtractGbRawData(orderHtml);

                // Create staging purchase order with gbRawData
                var stagingOrder = CreateStagingPurchaseOrder(batch.Id, orderData, orderRawData);
                Context.StagingPurchaseOrders.Add(stagingOrder);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to scrape order: {OrderLink}", orderLink);
                // Continue with next order
            }
        }

        await Context.SaveChangesAsync(cancellationToken);
        Logger.LogInformation("Saved {Count} staging purchase orders", orderLinks.Count);
    }

    /// <inheritdoc/>
    protected override async Task<bool> ValidateCookiesAsync(CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogInformation("Validating cookies for domain {Domain}", CookieFile!.Domain);

            var html = await ScrapePageAsync(
                PageType.AccountPage,
                Configuration.AccountPageUrlTemplate,
                cancellationToken);

            // Check for gbRawData which indicates successful authentication
            var isValid = html.Contains("gbRawData");

            Logger.LogInformation("Cookie validation result: {IsValid}", isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating cookies");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override IEnumerable<string> ExtractOrderLinks(string ordersListHtml)
    {
        Logger.LogDebug("Extracting order links from HTML (length: {Length})", ordersListHtml.Length);

        var links = new List<string>();

        // Simple regex-based extraction (to be replaced with proper HTML parsing)
        var pattern = @"href=""(/user/orders/detail\?[^""]+)""";
        var matches = Regex.Matches(ordersListHtml, pattern);

        foreach (Match match in matches)
        {
            if (match.Groups.Count > 1)
            {
                var relativePath = match.Groups[1].Value;
                var absoluteUrl = $"https://{Configuration.Domain}{relativePath}";
                if (!links.Contains(absoluteUrl))
                {
                    links.Add(absoluteUrl);
                }
            }
        }

        Logger.LogInformation("Extracted {Count} unique order links", links.Count);
        return links;
    }

    /// <inheritdoc/>
    protected override Task<Dictionary<string, object>> ParseOrderDetailsAsync(string orderDetailHtml, CancellationToken cancellationToken)
    {
        Logger.LogDebug("Parsing order details from HTML (length: {Length})", orderDetailHtml.Length);

        // TODO: Implement proper HTML parsing using HtmlAgilityPack or AngleSharp
        // For now, return basic metadata

        var orderData = new Dictionary<string, object>
        {
            { "html_length", orderDetailHtml.Length },
            { "scraped_at", DateTimeOffset.UtcNow.ToString("o") },
            { "has_gbRawData", orderDetailHtml.Contains("gbRawData") }
        };

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

    private static StagingPurchaseOrder CreateStagingPurchaseOrder(
        Guid batchId,
        Dictionary<string, object> orderData,
        string? gbRawData)
    {
        return new StagingPurchaseOrder
        {
            Id = Guid.NewGuid(),
            StagingBatchId = batchId,
            SupplierReference = orderData.ContainsKey("order_id")
                ? orderData["order_id"].ToString() ?? "UNKNOWN"
                : "UNKNOWN",
            OrderDate = orderData.ContainsKey("order_date")
                ? DateTimeOffset.Parse(orderData["order_date"].ToString()!)
                : DateTimeOffset.UtcNow,
            RawData = gbRawData ?? JsonSerializer.Serialize(orderData),
            IsImported = false
        };
    }
}
