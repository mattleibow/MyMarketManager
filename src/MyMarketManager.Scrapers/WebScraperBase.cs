using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMarketManager.Data;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
using MyMarketManager.Scrapers.Core;

namespace MyMarketManager.Scrapers;

/// <summary>
/// Abstract base class for web scrapers that extract data from supplier websites.
/// Provides common orchestration logic for scraping operations.
/// </summary>
public abstract class WebScraperBase
{
    protected readonly MyMarketManagerDbContext Context;
    protected readonly ILogger Logger;
    protected readonly ScraperConfiguration Configuration;

    protected WebScraperBase(
        MyMarketManagerDbContext context,
        ILogger logger,
        IOptions<ScraperConfiguration> configuration)
    {
        Context = context;
        Logger = logger;
        Configuration = configuration.Value;
    }

    /// <summary>
    /// Scrapes orders from the supplier's website and creates a staging batch.
    /// </summary>
    /// <param name="supplierId">The supplier ID for this scraping session.</param>
    /// <param name="cookies">The cookie file containing authentication cookies.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ScrapeAsync(Guid supplierId, CookieFile cookies, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Starting scraping for supplier {SupplierId}", supplierId);

        // Create scraper session
        var session = new ScraperSession
        {
            Id = Guid.NewGuid(),
            SupplierId = supplierId,
            StartedAt = DateTimeOffset.UtcNow,
            Status = ProcessingStatus.Queued,
            CookieFileJson = JsonSerializer.Serialize(cookies, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            })
        };

        Context.ScraperSessions.Add(session);
        await Context.SaveChangesAsync(cancellationToken);
        Logger.LogInformation("Created scraper session {SessionId}", session.Id);

        StagingBatch? batch = null;

        try
        {
            session.Status = ProcessingStatus.Started;
            await Context.SaveChangesAsync(cancellationToken);

            // Validate cookies first
            var cookiesValid = await ValidateCookiesAsync(cookies, cancellationToken);
            if (!cookiesValid)
            {
                throw new InvalidOperationException("Cookies are not valid or have expired");
            }

            // Create staging batch
            batch = await CreateStagingBatchAsync(supplierId, cookies, cancellationToken);

            // Execute the scraping logic
            await ExecuteScrapingAsync(batch, cookies, cancellationToken);

            // Mark as complete
            batch.Status = ProcessingStatus.Completed;
            session.Status = ProcessingStatus.Completed;
            session.CompletedAt = DateTimeOffset.UtcNow;
            session.StagingBatchId = batch.Id;
            batch.ScraperSessionId = session.Id;

            await Context.SaveChangesAsync(cancellationToken);

            Logger.LogInformation("Successfully completed scraping. Staging batch ID: {BatchId}", batch.Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to scrape orders");
            session.Status = ProcessingStatus.Failed;
            session.ErrorMessage = ex.Message;
            
            if (batch != null)
            {
                batch.Status = ProcessingStatus.Failed;
                batch.ErrorMessage = ex.Message;
            }

            await Context.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Validates that the cookies are still valid by loading a page and checking the response.
    /// </summary>
    protected virtual async Task<bool> ValidateCookiesAsync(CookieFile cookies, CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogInformation("Validating cookies for domain {Domain}", cookies.Domain);

            var html = await ScrapePageAsync(GetAccountPageUrl(), cookies, cancellationToken);

            // Call the derived class to validate the page content
            var isValid = await ValidatePageAsync(html, cancellationToken);

            Logger.LogInformation("Cookie validation result: {IsValid}", isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating cookies");
            return false;
        }
    }

    /// <summary>
    /// Validates a page's HTML content to check if cookies are still valid.
    /// Default implementation checks if response is not empty.
    /// Override this to provide supplier-specific validation logic.
    /// </summary>
    protected virtual Task<bool> ValidatePageAsync(string html, CancellationToken cancellationToken)
    {
        var isValid = !string.IsNullOrWhiteSpace(html) && html.Length > 100;
        return Task.FromResult(isValid);
    }

    /// <summary>
    /// Creates a staging batch for this scraping session.
    /// </summary>
    protected virtual async Task<StagingBatch> CreateStagingBatchAsync(Guid supplierId, CookieFile cookies, CancellationToken cancellationToken)
    {
        var batch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            UploadDate = DateTimeOffset.UtcNow,
            FileHash = ComputeFileHash(cookies),
            Status = ProcessingStatus.Queued,
            Notes = $"Scraped at {DateTimeOffset.UtcNow}"
        };

        Context.StagingBatches.Add(batch);
        await Context.SaveChangesAsync(cancellationToken);

        return batch;
    }

    /// <summary>
    /// Scrapes a specific page and returns the raw HTML.
    /// </summary>
    protected virtual async Task<string> ScrapePageAsync(string url, CookieFile cookies, CancellationToken cancellationToken)
    {
        Logger.LogDebug("Scraping from {Url}", url);

        using var client = CreateHttpClient(cookies);

        var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <summary>
    /// Creates an HttpClient configured with cookies and headers for scraping.
    /// Virtual to allow mocking in tests.
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
                Logger.LogWarning(ex, "Failed to add cookie {CookieName}", cookie.Name);
            }
        }

        var client = new HttpClient(handler)
        {
            Timeout = Configuration.RequestTimeout
        };

        // Add headers
        client.DefaultRequestHeaders.Add("user-agent", Configuration.UserAgent);
        foreach (var header in Configuration.AdditionalHeaders)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
        }

        return client;
    }

    /// <summary>
    /// Executes the main scraping logic. Default implementation follows the pattern:
    /// 1. Fetch orders list page
    /// 2. Parse order links
    /// 3. Loop through each order link and scrape details
    /// Override this to provide custom scraping logic.
    /// </summary>
    protected virtual async Task ExecuteScrapingAsync(StagingBatch batch, CookieFile cookies, CancellationToken cancellationToken)
    {
        // Step 1: Scrape orders list page
        Logger.LogInformation("Fetching orders list page");
        var ordersListHtml = await ScrapePageAsync(GetOrdersListUrl(), cookies, cancellationToken);

        // Step 2: Parse order links
        Logger.LogInformation("Parsing orders from list page");
        var orderLinks = ParseOrdersListAsync(ordersListHtml).ToList();
        Logger.LogInformation("Found {Count} orders", orderLinks.Count);

        // Step 3: Scrape each order detail page
        foreach (var orderLinkInfo in orderLinks)
        {
            StagingPurchaseOrder? stagingOrder = null;
            string? orderUrl = null;

            try
            {
                await Task.Delay(Configuration.RequestDelay, cancellationToken);

                // Try to build the order URL
                orderUrl = GetOrderDetailUrl(orderLinkInfo);
                Logger.LogInformation("Scraping order: {OrderUrl}", orderUrl);

                // Create the staging order immediately with the URL as reference
                stagingOrder = new StagingPurchaseOrder
                {
                    Id = Guid.NewGuid(),
                    StagingBatchId = batch.Id,
                    SupplierReference = orderUrl,
                    OrderDate = DateTimeOffset.UtcNow,
                    RawData = JsonSerializer.Serialize(orderLinkInfo),
                    IsImported = false,
                    Status = ProcessingStatus.Started
                };
                Context.StagingPurchaseOrders.Add(stagingOrder);
                await Context.SaveChangesAsync(cancellationToken);

                // Scrape the order page
                var orderHtml = await ScrapePageAsync(orderUrl, cookies, cancellationToken);

                // Parse order details
                var orderData = await ParseOrderDetailsAsync(orderHtml, cancellationToken);

                // Update staging order with parsed data
                stagingOrder.SupplierReference = orderData.ContainsKey("order_id")
                    ? orderData["order_id"].ToString() ?? orderUrl
                    : orderUrl;
                stagingOrder.OrderDate = orderData.ContainsKey("order_date")
                    ? DateTimeOffset.Parse(orderData["order_date"].ToString()!)
                    : DateTimeOffset.UtcNow;
                stagingOrder.RawData = orderData.ContainsKey("raw_data")
                    ? orderData["raw_data"].ToString() ?? JsonSerializer.Serialize(orderData)
                    : JsonSerializer.Serialize(orderData);
                stagingOrder.Status = ProcessingStatus.Completed;

                await Context.SaveChangesAsync(cancellationToken);
                Logger.LogInformation("Successfully scraped order: {OrderUrl}", orderUrl);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to scrape order: {OrderUrl}", orderUrl ?? "unknown");

                if (stagingOrder != null)
                {
                    // Record the failure in the order
                    stagingOrder.Status = ProcessingStatus.Failed;
                    stagingOrder.ErrorMessage = ex.Message;
                    await Context.SaveChangesAsync(cancellationToken);
                }
                else if (orderUrl != null)
                {
                    // We have the URL but failed before creating the order - create failed order record
                    stagingOrder = new StagingPurchaseOrder
                    {
                        Id = Guid.NewGuid(),
                        StagingBatchId = batch.Id,
                        SupplierReference = orderUrl,
                        OrderDate = DateTimeOffset.UtcNow,
                        RawData = JsonSerializer.Serialize(orderLinkInfo),
                        IsImported = false,
                        Status = ProcessingStatus.Failed,
                        ErrorMessage = ex.Message
                    };
                    Context.StagingPurchaseOrders.Add(stagingOrder);
                    await Context.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    // Failed to get URL - this is a fatal error for the whole batch
                    Logger.LogCritical(ex, "Failed to build order URL from link info: {OrderLinkInfo}", orderLinkInfo);
                    throw;
                }
            }
        }

        Logger.LogInformation("Completed scraping {Count} orders", orderLinks.Count);
    }

    /// <summary>
    /// Replaces template placeholders in a URL with actual values from the dictionary.
    /// </summary>
    protected string ReplaceUrlTemplateValues(string template, Dictionary<string, string> values)
    {
        var result = template;
        foreach (var kvp in values)
        {
            result = result.Replace($"{{{kvp.Key}}}", kvp.Value);
        }
        return result;
    }

    /// <summary>
    /// Gets the account page URL used for cookie validation.
    /// Must be implemented by derived classes.
    /// </summary>
    protected abstract string GetAccountPageUrl();

    /// <summary>
    /// Gets the orders list page URL.
    /// Must be implemented by derived classes.
    /// </summary>
    protected abstract string GetOrdersListUrl();

    /// <summary>
    /// Gets the order detail URL from the parsed link information.
    /// Must be implemented by derived classes.
    /// </summary>
    protected abstract string GetOrderDetailUrl(Dictionary<string, string> orderLinkInfo);

    /// <summary>
    /// Parses the orders list page and extracts information for each order.
    /// Returns a dictionary of values for each order (e.g., {"orderId": "12345"}).
    /// Must be implemented by derived classes.
    /// </summary>
    protected abstract IEnumerable<Dictionary<string, string>> ParseOrdersListAsync(string ordersListHtml);

    /// <summary>
    /// Parses order details from an order detail page. Must be implemented by derived classes.
    /// </summary>
    protected abstract Task<Dictionary<string, object>> ParseOrderDetailsAsync(string orderDetailHtml, CancellationToken cancellationToken);

    private static string ComputeFileHash(CookieFile cookieFile)
    {
        var json = JsonSerializer.Serialize(cookieFile, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(hashBytes);
    }
}
