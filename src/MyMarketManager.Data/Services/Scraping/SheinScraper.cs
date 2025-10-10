using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Services.Scraping;

/// <summary>
/// Web scraper implementation for Shein.com orders.
/// </summary>
public class SheinScraper : IWebScraper
{
    private readonly MyMarketManagerDbContext _context;
    private readonly ILogger<SheinScraper> _logger;

    public SheinScraper(MyMarketManagerDbContext context, ILogger<SheinScraper> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public ScraperConfiguration Configuration { get; } = new ScraperConfiguration
    {
        SupplierName = "Shein",
        Domain = "shein.com",
        OrdersListUrl = "https://shein.com/user/orders/list",
        OrderDetailUrlPattern = "https://shein.com/user/orders/detail?order_id={orderId}",
        ProductPageUrlPattern = "https://shein.com/product/{productId}",
        AccountPageUrl = "https://shein.com/user/account",
        OrderLinkSelector = "a[href*='/user/orders/detail']",
        OrderIdSelector = ".order-id, [data-order-id]",
        OrderDateSelector = ".order-date, [data-order-date]",
        OrderItemsSelector = ".order-item, [data-order-item]",
        LoggedInIndicatorSelector = ".user-info, [data-user-logged-in]",
        UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36 Edg/141.0.0.0",
        AdditionalHeaders = new Dictionary<string, string>
        {
            { "accept", "text/html" },
            { "accept-language", "en-US" },
            { "cache-control", "no-cache" },
            { "upgrade-insecure-requests", "1" }
        },
        RequestDelayMs = 2000,
        MaxConcurrentRequests = 1,
        RequestTimeoutSeconds = 30,
        RequiresHeadlessBrowser = false,
        Notes = "Shein orders are listed at /user/orders/list. May require JavaScript execution for full page rendering. Check for 'gbRawData' in response to verify successful authentication."
    };

    /// <inheritdoc/>
    public async Task<Guid> ScrapeOrdersAsync(CookieFile cookieFile, DateTimeOffset? lastSuccessfulScrape, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Shein order scraping for supplier {SupplierId}", cookieFile.SupplierId);

        // Validate cookies first
        var cookiesValid = await ValidateCookiesAsync(cookieFile, cancellationToken);
        if (!cookiesValid)
        {
            _logger.LogWarning("Cookie validation failed for supplier {SupplierId}", cookieFile.SupplierId);
            throw new InvalidOperationException("Cookies are not valid or have expired");
        }

        // Create staging batch
        var batch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            SupplierId = cookieFile.SupplierId,
            UploadDate = DateTimeOffset.UtcNow,
            FileHash = ComputeFileHash(cookieFile),
            Status = ProcessingStatus.Pending,
            Notes = $"Scraped from Shein at {DateTimeOffset.UtcNow}. Last successful scrape: {lastSuccessfulScrape?.ToString() ?? "Never"}"
        };

        _context.StagingBatches.Add(batch);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            // Step 1: Scrape orders list page
            _logger.LogInformation("Fetching orders list page");
            var ordersListHtml = await ScrapePageAsync(PageType.OrdersListPage, cookieFile, null, cancellationToken);

            // Step 2: Extract order links
            _logger.LogInformation("Extracting order links from orders list");
            var orderLinks = ExtractOrderLinks(ordersListHtml).ToList();
            _logger.LogInformation("Found {Count} order links", orderLinks.Count);

            // Step 3: Scrape each order detail page
            foreach (var orderLink in orderLinks)
            {
                try
                {
                    await Task.Delay(Configuration.RequestDelayMs, cancellationToken);

                    _logger.LogInformation("Scraping order: {OrderLink}", orderLink);
                    var orderHtml = await ScrapePageAsync(PageType.OrderDetailsPage, cookieFile, orderLink, cancellationToken);

                    // Parse order details
                    var orderData = await ParseOrderDetailsAsync(orderHtml, cancellationToken);

                    // Create staging purchase order
                    var stagingOrder = CreateStagingPurchaseOrder(batch.Id, orderData, orderHtml);
                    _context.StagingPurchaseOrders.Add(stagingOrder);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to scrape order: {OrderLink}", orderLink);
                    // Continue with next order
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            batch.Status = ProcessingStatus.Complete;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully completed scraping. Staging batch ID: {BatchId}", batch.Id);
            return batch.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scrape orders");
            batch.Status = ProcessingStatus.Pending;
            batch.Notes += $"\nError: {ex.Message}";
            await _context.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateCookiesAsync(CookieFile cookieFile, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating cookies for domain {Domain}", cookieFile.Domain);

            var html = await ScrapePageAsync(PageType.AccountPage, cookieFile, null, cancellationToken);

            // Check for logged-in indicator or gbRawData
            var isValid = html.Contains("gbRawData") || html.Contains(Configuration.LoggedInIndicatorSelector);

            _logger.LogInformation("Cookie validation result: {IsValid}", isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating cookies");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<string> ScrapePageAsync(PageType pageType, CookieFile cookieFile, string? pageUrl = null, CancellationToken cancellationToken = default)
    {
        var url = pageUrl ?? GetDefaultUrl(pageType);
        _logger.LogDebug("Scraping {PageType} from {Url}", pageType, url);

        using var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new CookieContainer()
        };

        // Add cookies to the container
        foreach (var cookie in cookieFile.Cookies)
        {
            try
            {
                handler.CookieContainer.Add(new Uri($"https://{Configuration.Domain}"), new Cookie
                {
                    Name = cookie.Name,
                    Value = cookie.Value,
                    Domain = cookie.Domain ?? $".{Configuration.Domain}",
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

        using var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(Configuration.RequestTimeoutSeconds)
        };

        // Add headers
        client.DefaultRequestHeaders.Add("user-agent", Configuration.UserAgent);
        foreach (var header in Configuration.AdditionalHeaders)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
        }

        var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public IEnumerable<string> ExtractOrderLinks(string ordersListHtml)
    {
        _logger.LogDebug("Extracting order links from HTML (length: {Length})", ordersListHtml.Length);

        // TODO: Implement proper HTML parsing using HtmlAgilityPack or AngleSharp
        // For now, this is a placeholder that looks for order detail URLs in the HTML

        var links = new List<string>();

        // Simple regex-based extraction (to be replaced with proper HTML parsing)
        var pattern = @"href=""(/user/orders/detail\?[^""]+)""";
        var matches = System.Text.RegularExpressions.Regex.Matches(ordersListHtml, pattern);

        foreach (System.Text.RegularExpressions.Match match in matches)
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

        _logger.LogInformation("Extracted {Count} unique order links", links.Count);
        return links;
    }

    /// <inheritdoc/>
    public Task<Dictionary<string, object>> ParseOrderDetailsAsync(string orderDetailHtml, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Parsing order details from HTML (length: {Length})", orderDetailHtml.Length);

        // TODO: Implement proper HTML parsing using HtmlAgilityPack or AngleSharp
        // For now, return a placeholder structure

        var orderData = new Dictionary<string, object>
        {
            { "html_length", orderDetailHtml.Length },
            { "scraped_at", DateTimeOffset.UtcNow.ToString("o") },
            { "has_gbRawData", orderDetailHtml.Contains("gbRawData") }
        };

        return Task.FromResult(orderData);
    }

    private string GetDefaultUrl(PageType pageType)
    {
        return pageType switch
        {
            PageType.OrdersListPage => Configuration.OrdersListUrl,
            PageType.AccountPage => Configuration.AccountPageUrl,
            PageType.OrderDetailsPage => throw new ArgumentException("OrderDetailsPage requires a specific URL"),
            PageType.ProductPage => throw new ArgumentException("ProductPage requires a specific URL"),
            _ => throw new ArgumentOutOfRangeException(nameof(pageType))
        };
    }

    private static string ComputeFileHash(CookieFile cookieFile)
    {
        // Simple hash based on cookie file content
        var json = JsonSerializer.Serialize(cookieFile);
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(hashBytes);
    }

    private static StagingPurchaseOrder CreateStagingPurchaseOrder(Guid batchId, Dictionary<string, object> orderData, string rawHtml)
    {
        return new StagingPurchaseOrder
        {
            Id = Guid.NewGuid(),
            StagingBatchId = batchId,
            SupplierReference = orderData.ContainsKey("order_id") ? orderData["order_id"].ToString() ?? "UNKNOWN" : "UNKNOWN",
            OrderDate = orderData.ContainsKey("order_date") ? DateTimeOffset.Parse(orderData["order_date"].ToString()!) : DateTimeOffset.UtcNow,
            RawData = JsonSerializer.Serialize(new
            {
                parsed_data = orderData,
                raw_html = rawHtml.Length > 10000 ? rawHtml.Substring(0, 10000) + "... (truncated)" : rawHtml
            }),
            IsImported = false
        };
    }
}
