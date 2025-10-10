using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Services.Scraping;

/// <summary>
/// Abstract base class for web scrapers that extract data from supplier websites.
/// Provides common orchestration logic for scraping operations.
/// </summary>
public abstract class WebScraperBase
{
    protected readonly MyMarketManagerDbContext Context;
    protected readonly ILogger Logger;
    protected readonly ScraperConfiguration Configuration;
    protected CookieFile? CookieFile;
    protected ScraperSession? Session;

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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ScrapeAsync(CancellationToken cancellationToken = default)
    {
        if (Session == null)
        {
            throw new InvalidOperationException("Scraper session not initialized. Call InitializeAsync first.");
        }

        if (CookieFile == null)
        {
            throw new InvalidOperationException("Cookie file not set. Call InitializeAsync first.");
        }

        Logger.LogInformation("Starting scraping for supplier {SupplierId}", CookieFile.SupplierId);

        try
        {
            Session.Status = ProcessingStatus.Started;
            await Context.SaveChangesAsync(cancellationToken);

            // Validate cookies first
            var cookiesValid = await ValidateCookiesAsync(cancellationToken);
            if (!cookiesValid)
            {
                throw new InvalidOperationException("Cookies are not valid or have expired");
            }

            // Create staging batch
            var batch = await CreateStagingBatchAsync(cancellationToken);

            // Execute the scraping logic
            await ExecuteScrapingAsync(batch, cancellationToken);

            // Mark as complete
            batch.Status = ProcessingStatus.Complete;
            Session.Status = ProcessingStatus.Completed;
            Session.CompletedAt = DateTimeOffset.UtcNow;
            Session.StagingBatchId = batch.Id;
            batch.ScraperSessionId = Session.Id;

            await Context.SaveChangesAsync(cancellationToken);

            Logger.LogInformation("Successfully completed scraping. Staging batch ID: {BatchId}", batch.Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to scrape orders");
            Session.Status = ProcessingStatus.Failed;
            Session.ErrorMessage = ex.Message;
            await Context.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Initializes the scraper with a cookie file and creates a session.
    /// </summary>
    public async Task InitializeAsync(CookieFile cookieFile, CancellationToken cancellationToken = default)
    {
        CookieFile = cookieFile;

        // Create scraper session
        Session = new ScraperSession
        {
            Id = Guid.NewGuid(),
            SupplierId = cookieFile.SupplierId,
            StartedAt = DateTimeOffset.UtcNow,
            Status = ProcessingStatus.Queued,
            CookieFileJson = JsonSerializer.Serialize(cookieFile, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            })
        };

        Context.ScraperSessions.Add(Session);
        await Context.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Created scraper session {SessionId}", Session.Id);
    }

    /// <summary>
    /// Validates that the cookies are still valid by loading a page and checking the response.
    /// </summary>
    protected virtual async Task<bool> ValidateCookiesAsync(CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogInformation("Validating cookies for domain {Domain}", CookieFile!.Domain);

            var html = await ScrapePageAsync(Configuration.AccountPageUrlTemplate, cancellationToken);

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
    protected virtual async Task<StagingBatch> CreateStagingBatchAsync(CancellationToken cancellationToken)
    {
        var batch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            SupplierId = CookieFile!.SupplierId,
            UploadDate = DateTimeOffset.UtcNow,
            FileHash = ComputeFileHash(CookieFile),
            Status = ProcessingStatus.Queued,
            Notes = $"Scraped from {Configuration.SupplierName} at {DateTimeOffset.UtcNow}"
        };

        Context.StagingBatches.Add(batch);
        await Context.SaveChangesAsync(cancellationToken);

        return batch;
    }

    /// <summary>
    /// Scrapes a specific page and returns the raw HTML.
    /// </summary>
    protected virtual async Task<string> ScrapePageAsync(string url, CancellationToken cancellationToken)
    {
        Logger.LogDebug("Scraping from {Url}", url);

        using var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new CookieContainer()
        };

        // Add cookies to the container
        foreach (var cookie in CookieFile!.Cookies.Values)
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
                Logger.LogWarning(ex, "Failed to add cookie {CookieName}", cookie.Name);
            }
        }

        using var client = new HttpClient(handler)
        {
            Timeout = Configuration.RequestTimeout
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

    /// <summary>
    /// Executes the main scraping logic. Default implementation follows the pattern:
    /// 1. Fetch orders list page
    /// 2. Extract order links
    /// 3. Loop through each order link and scrape details
    /// Override this to provide custom scraping logic.
    /// </summary>
    protected virtual async Task ExecuteScrapingAsync(StagingBatch batch, CancellationToken cancellationToken)
    {
        // Step 1: Scrape orders list page
        Logger.LogInformation("Fetching orders list page");
        var ordersListHtml = await ScrapePageAsync(Configuration.OrdersListUrlTemplate, cancellationToken);

        // Step 2: Extract order links
        Logger.LogInformation("Extracting order links from orders list");
        var orderLinks = ExtractOrderLinks(ordersListHtml).ToList();
        Logger.LogInformation("Found {Count} order links", orderLinks.Count);

        // Step 3: Scrape each order detail page
        foreach (var orderLinkInfo in orderLinks)
        {
            try
            {
                await Task.Delay(Configuration.RequestDelay, cancellationToken);

                var orderUrl = ReplaceUrlTemplateValues(Configuration.OrderDetailUrlTemplate, orderLinkInfo);
                Logger.LogInformation("Scraping order: {OrderUrl}", orderUrl);
                
                var orderHtml = await ScrapePageAsync(orderUrl, cancellationToken);

                // Parse order details
                var orderData = await ParseOrderDetailsAsync(orderHtml, cancellationToken);

                // Create staging purchase order
                var stagingOrder = await CreateStagingOrderAsync(batch.Id, orderData, orderHtml, cancellationToken);
                Context.StagingPurchaseOrders.Add(stagingOrder);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to scrape order with info: {OrderLinkInfo}", orderLinkInfo);
                // Continue with next order
            }
        }

        await Context.SaveChangesAsync(cancellationToken);
        Logger.LogInformation("Saved {Count} staging purchase orders", orderLinks.Count);
    }

    /// <summary>
    /// Creates a staging purchase order from scraped data.
    /// Override this to customize how orders are created.
    /// </summary>
    protected virtual Task<StagingPurchaseOrder> CreateStagingOrderAsync(
        Guid batchId,
        Dictionary<string, object> orderData,
        string rawHtml,
        CancellationToken cancellationToken)
    {
        var order = new StagingPurchaseOrder
        {
            Id = Guid.NewGuid(),
            StagingBatchId = batchId,
            SupplierReference = orderData.ContainsKey("order_id")
                ? orderData["order_id"].ToString() ?? "UNKNOWN"
                : "UNKNOWN",
            OrderDate = orderData.ContainsKey("order_date")
                ? DateTimeOffset.Parse(orderData["order_date"].ToString()!)
                : DateTimeOffset.UtcNow,
            RawData = orderData.ContainsKey("raw_data")
                ? orderData["raw_data"].ToString() ?? JsonSerializer.Serialize(orderData)
                : JsonSerializer.Serialize(orderData),
            IsImported = false
        };

        return Task.FromResult(order);
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
    /// Extracts order links from the orders list page.
    /// Returns a dictionary of template values for each order (e.g., {"orderId": "12345"}).
    /// Must be implemented by derived classes.
    /// </summary>
    protected abstract IEnumerable<Dictionary<string, string>> ExtractOrderLinks(string ordersListHtml);

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
