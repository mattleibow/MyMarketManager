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
public abstract class WebScraperBase(
    MyMarketManagerDbContext context,
    ILogger logger,
    IOptions<ScraperConfiguration> configuration)
{
    protected MyMarketManagerDbContext Context { get; } = context;

    protected ILogger Logger { get; } = logger;

    protected ScraperConfiguration Configuration { get; } = configuration.Value;

    private HttpClient? _httpClient;

    /// <summary>
    /// Gets the account page URL used for cookie validation.
    /// </summary>
    protected abstract string GetAccountPageUrl();

    /// <summary>
    /// Gets the orders list page URL.
    /// </summary>
    protected abstract string GetOrdersListUrl();

    /// <summary>
    /// Gets the order detail URL from the parsed link information.
    /// </summary>
    protected abstract string GetOrderDetailUrl(WebScraperOrderSummary order);

    /// <summary>
    /// Parses the orders list page and extracts information for each order.
    /// Returns a dictionary of values for each order (e.g., {"orderId": "12345"}).
    /// </summary>
    protected abstract IEnumerable<WebScraperOrderSummary> ParseOrdersListAsync(string ordersListHtml);

    /// <summary>
    /// Parses order details from an order detail page.
    /// </summary>
    protected abstract Task<WebScraperOrder> ParseOrderDetailsAsync(string orderDetailHtml, WebScraperOrderSummary orderSummary, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the staging order entity with data from the scraped order.
    /// </summary>
    protected abstract Task UpdateStagingOrderAsync(StagingPurchaseOrder stagingOrder, WebScraperOrder order, CancellationToken cancellationToken);

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

        var sessionId = Guid.NewGuid();
        var batchId = Guid.NewGuid();

        // Create scraper session
        var session = new ScraperSession
        {
            Id = sessionId,
            SupplierId = supplierId,
            StartedAt = DateTimeOffset.UtcNow,
            Status = ProcessingStatus.Started,
            CookieFileJson = JsonSerializer.Serialize(cookies, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            }),
            StagingBatchId = batchId
        };
        Context.ScraperSessions.Add(session);

        // Create staging batch
        var batch = new StagingBatch
        {
            Id = batchId,
            UploadDate = DateTimeOffset.UtcNow,
            FileHash = ComputeFileHash(cookies),
            Status = ProcessingStatus.Started,
            Notes = $"Scraped at {DateTimeOffset.UtcNow}",
            ScraperSessionId = sessionId
        };
        Context.StagingBatches.Add(batch);

        await Context.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Started scraper session {SessionId} and staging batch {BatchId}", sessionId, batchId);

        try
        {
            // Validate cookies first
            var cookiesValid = await ValidateCookiesAsync(cookies, cancellationToken);
            if (!cookiesValid)
            {
                throw new InvalidOperationException("Cookies are not valid or have expired");
            }

            // Execute the scraping logic
            await ExecuteScrapingAsync(batch, cookies, cancellationToken);

            // Mark as complete
            batch.Status = ProcessingStatus.Completed;
            session.Status = ProcessingStatus.Completed;
            session.CompletedAt = DateTimeOffset.UtcNow;

            await Context.SaveChangesAsync(cancellationToken);

            Logger.LogInformation("Successfully completed scraping. Scraper session {SessionId} and staging batch {BatchId}", sessionId, batchId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to scrape orders");

            batch.Status = ProcessingStatus.Failed;
            batch.ErrorMessage = ex.Message;

            session.Status = ProcessingStatus.Failed;
            session.ErrorMessage = ex.Message;

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

            var html = await FetchPageAsync(GetAccountPageUrl(), cookies, cancellationToken);

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
        var ordersListHtml = await FetchPageAsync(GetOrdersListUrl(), cookies, cancellationToken);

        // Step 2: Parse order links
        Logger.LogInformation("Parsing orders from list page");
        var orderSummaries = ParseOrdersListAsync(ordersListHtml).ToList();
        Logger.LogInformation("Found {Count} orders", orderSummaries.Count);

        // Step 3: Scrape each order detail page
        foreach (var orderSummary in orderSummaries)
        {
            await Task.Delay(Configuration.RequestDelay, cancellationToken);

            // Build order detail URL
            var orderUrl = GetOrderDetailUrl(orderSummary);
            Logger.LogInformation("Scraping order: {OrderUrl}", orderUrl);

            // Create the staging order immediately with the URL as reference
            var stagingOrder = new StagingPurchaseOrder
            {
                Id = Guid.NewGuid(),
                StagingBatchId = batch.Id,
                SupplierReference = orderUrl,
                OrderDate = DateTimeOffset.UtcNow,
                RawData = JsonSerializer.Serialize(orderSummary),
                IsImported = false,
                Status = ProcessingStatus.Started
            };
            Context.StagingPurchaseOrders.Add(stagingOrder);
            await Context.SaveChangesAsync(cancellationToken);
            Logger.LogInformation("Created staging order {StagingOrderId} for URL {OrderUrl}", stagingOrder.Id, orderUrl);

            try
            {
                // Scrape the order page
                var orderDetailsHtml = await FetchPageAsync(orderUrl, cookies, cancellationToken);

                // Parse order details
                var order = await ParseOrderDetailsAsync(orderDetailsHtml, orderSummary, cancellationToken);

                // Update staging order with parsed data
                await UpdateStagingOrderAsync(stagingOrder, order, cancellationToken);

                stagingOrder.Status = ProcessingStatus.Completed;
                await Context.SaveChangesAsync(cancellationToken);

                Logger.LogInformation("Successfully scraped order: {OrderUrl}", orderUrl);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to scrape order: {OrderUrl}", orderUrl ?? "unknown");

                // Record the failure in the order
                stagingOrder.Status = ProcessingStatus.Failed;
                stagingOrder.ErrorMessage = ex.Message;
                await Context.SaveChangesAsync(cancellationToken);
            }
        }

        Logger.LogInformation("Completed scraping {Count} orders", orderSummaries.Count);
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
    /// Scrapes a specific page and returns the raw HTML.
    /// </summary>
    protected virtual async Task<string> FetchPageAsync(string url, CookieFile cookies, CancellationToken cancellationToken)
    {
        Logger.LogDebug("Fetching from {Url}", url);

        // Create and cache the HTTP client on first use
        _httpClient ??= CreateHttpClient(cookies);

        var response = await _httpClient.GetAsync(url, cancellationToken);
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

public class WebScraperOrderSummary : Dictionary<string, string>
{
    public string? RawData { get; set; }
}

public class WebScraperOrder(WebScraperOrderSummary orderSummary) : Dictionary<string, string>
{
    public WebScraperOrderSummary OrderSummary { get; } = orderSummary;

    public List<WebScraperOrderItem> OrderItems { get; set; } = new();

    public string? RawData { get; set; }
}

public class WebScraperOrderItem(WebScraperOrder order) : Dictionary<string, string>
{
    public WebScraperOrder Order { get; } = order;

    public string? RawData { get; set; }
}
