using System.Net;
using System.Runtime.CompilerServices;
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
public abstract class WebScraper(
    MyMarketManagerDbContext context,
    ILogger logger,
    IOptions<ScraperConfiguration> configuration)
{
    protected MyMarketManagerDbContext Context { get; } = context;

    protected ILogger Logger { get; } = logger;

    protected ScraperConfiguration Configuration { get; } = configuration.Value;

    /// <summary>
    /// Gets the orders list page URL.
    /// </summary>
    public abstract string GetOrdersListUrl();

    /// <summary>
    /// Gets the order detail URL from the parsed link information.
    /// </summary>
    public abstract string GetOrderDetailUrl(WebScraperOrderSummary order);

    /// <summary>
    /// Parses the orders list page and extracts information for each order.
    /// Returns a dictionary of values for each order (e.g., {"orderId": "12345"}).
    /// </summary>
    public abstract IAsyncEnumerable<WebScraperOrderSummary> ParseOrdersListAsync(string ordersListHtml, CancellationToken cancellationToken);

    /// <summary>
    /// Parses order details from an order detail page.
    /// </summary>
    public abstract Task<WebScraperOrder> ParseOrderDetailsAsync(string orderDetailHtml, WebScraperOrderSummary orderSummary, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the staging purchase order entity with data from the scraped order.
    /// </summary>
    public abstract Task UpdateStagingPurchaseOrderAsync(StagingPurchaseOrder stagingOrder, WebScraperOrder order, CancellationToken cancellationToken);

    /// <summary>
    /// Scrapes orders from the supplier's website and creates a staging batch.
    /// </summary>
    /// <param name="supplierId">The supplier ID for this scraping session.</param>
    /// <param name="cookies">The cookie file containing authentication cookies.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StartScrapingAsync(Guid supplierId, CookieFile? cookies, CancellationToken cancellationToken = default)
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
            CookieFileJson = JsonSerializer.Serialize(cookies, JsonSerializerOptions.Web),
            StagingBatchId = batchId
        };
        Context.ScraperSessions.Add(session);

        // Create staging batch
        var batch = new StagingBatch
        {
            Id = batchId,
            UploadDate = DateTimeOffset.UtcNow,
            FileHash = ComputeFileHash(session.CookieFileJson),
            Status = ProcessingStatus.Started,
            Notes = $"Scraped at {DateTimeOffset.UtcNow}",
            ScraperSessionId = sessionId
        };
        Context.StagingBatches.Add(batch);

        await Context.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Started scraper session {SessionId} and staging batch {BatchId}", sessionId, batchId);

        try
        {
            // Execute the scraping logic
            await ScrapeBatchAsync(batch, cancellationToken);

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
    /// Executes the main scraping logic:<br/>
    /// 1. Fetch orders list page<br/>
    /// 2. Parse order links<br/>
    /// 3. Loop through each order link and scrape details
    /// </summary>
    public async Task ScrapeBatchAsync(StagingBatch batch, CancellationToken cancellationToken)
    {
        // Step 0: Create HttpClient with cookies and headers
        var cookies = JsonSerializer.Deserialize<CookieFile>(batch.ScraperSession?.CookieFileJson ?? "{}", JsonSerializerOptions.Web) ?? new CookieFile();
        using var httpClient = CreateHttpClient(cookies);

        // Step 1: Scrape orders list page
        Logger.LogInformation("Fetching orders list page");
        var ordersListHtml = await FetchPageAsync(GetOrdersListUrl(), httpClient, cancellationToken);

        // Step 2: Parse order links
        Logger.LogInformation("Parsing orders from list page");
        var orderSummaries = ParseOrdersListAsync(ordersListHtml, cancellationToken);

        // Step 3: Scrape each order detail page
        var orderCount = 0;
        await foreach (var orderSummary in orderSummaries)
        {
            orderCount++;

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
                var orderDetailsHtml = await FetchPageAsync(orderUrl, httpClient, cancellationToken);

                // Parse order details
                var order = await ParseOrderDetailsAsync(orderDetailsHtml, orderSummary, cancellationToken);

                // Update staging order with parsed data
                await UpdateStagingPurchaseOrderAsync(stagingOrder, order, cancellationToken);

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

        Logger.LogInformation("Completed scraping {Count} orders", orderCount);
    }

    /// <summary>
    /// Scrapes a specific page and returns the raw HTML.
    /// </summary>
    public async Task<string> FetchPageAsync(string url, HttpClient httpClient, CancellationToken cancellationToken)
    {
        Logger.LogDebug("Fetching from {Url}", url);

        var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <summary>
    /// Creates an HttpClient configured with cookies and headers for scraping.
    /// </summary>
    public virtual HttpClient CreateHttpClient(CookieFile cookies)
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

        // Create the actual HttpClient
        var client = CreateHttpClient(handler);

        // Set timeouts
        client.Timeout = Configuration.RequestTimeout;

        // Add headers
        client.DefaultRequestHeaders.Add("user-agent", Configuration.UserAgent);
        foreach (var header in Configuration.AdditionalHeaders)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
        }

        return client;
    }

    /// <summary>
    /// Creates an HttpClient instance. Can be overridden for testing/mocking.
    /// </summary>
    public virtual HttpClient CreateHttpClient(HttpClientHandler handler) =>
        new HttpClient(handler);

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

    private static string ComputeFileHash(string? json)
    {
        ArgumentNullException.ThrowIfNull(json);

        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(hashBytes);
    }
}
