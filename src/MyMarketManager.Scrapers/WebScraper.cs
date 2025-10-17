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
    IOptions<ScraperConfiguration> configuration,
    IWebScraperSessionFactory sessionFactory)
{
    protected MyMarketManagerDbContext Context { get; } = context;

    protected ILogger Logger { get; } = logger;

    protected ScraperConfiguration Configuration { get; } = configuration.Value;

    protected IWebScraperSessionFactory SessionFactory { get; } = sessionFactory;

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

        var batchId = Guid.NewGuid();

        // Create staging batch for web scrape
        var batch = new StagingBatch
        {
            Id = batchId,
            BatchType = StagingBatchType.WebScrape,
            SupplierId = supplierId,
            StartedAt = DateTimeOffset.UtcNow,
            Status = ProcessingStatus.Started,
            FileContents = JsonSerializer.Serialize(cookies, JsonSerializerOptions.Web),
            FileHash = ComputeFileHash(JsonSerializer.Serialize(cookies, JsonSerializerOptions.Web)),
            Notes = $"Scraped at {DateTimeOffset.UtcNow}"
        };
        Context.StagingBatches.Add(batch);

        await Context.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Started scraping staging batch {BatchId}", batchId);

        try
        {
            // Execute the scraping logic
            await ScrapeBatchAsync(batch, cancellationToken);

            // Mark as complete
            batch.Status = ProcessingStatus.Completed;
            batch.CompletedAt = DateTimeOffset.UtcNow;

            await Context.SaveChangesAsync(cancellationToken);

            Logger.LogInformation("Successfully completed scraping. Staging batch {BatchId}", batchId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to scrape orders");

            batch.Status = ProcessingStatus.Failed;
            batch.ErrorMessage = ex.Message;

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
        // Step 0: Create scraping session with cookies
        var cookies = JsonSerializer.Deserialize<CookieFile>(batch.FileContents ?? "{}", JsonSerializerOptions.Web) ?? new CookieFile();
        using var session = SessionFactory.CreateSession(cookies);

        // Step 1: Scrape orders list page
        Logger.LogInformation("Fetching orders list page");
        var ordersListHtml = await session.FetchPageAsync(GetOrdersListUrl(), cancellationToken);

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
                var orderDetailsHtml = await session.FetchPageAsync(orderUrl, cancellationToken);

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
