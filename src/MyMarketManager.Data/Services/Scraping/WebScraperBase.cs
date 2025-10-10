using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Services.Scraping;

/// <summary>
/// Abstract base class for web scrapers with common orchestration logic.
/// </summary>
public abstract class WebScraperBase : IWebScraper
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

    /// <inheritdoc/>
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
    /// Validates that the cookies are still valid.
    /// </summary>
    protected virtual async Task<bool> ValidateCookiesAsync(CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogInformation("Validating cookies for domain {Domain}", CookieFile!.Domain);

            var html = await ScrapePageAsync(PageType.AccountPage, Configuration.AccountPageUrlTemplate, cancellationToken);

            // Default validation: check if response is not empty and doesn't contain error indicators
            var isValid = !string.IsNullOrWhiteSpace(html) && html.Length > 100;

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
            Status = ProcessingStatus.Pending,
            Notes = $"Scraped from {Configuration.SupplierName} at {DateTimeOffset.UtcNow}"
        };

        Context.StagingBatches.Add(batch);
        await Context.SaveChangesAsync(cancellationToken);

        return batch;
    }

    /// <summary>
    /// Scrapes a specific page and returns the raw HTML.
    /// </summary>
    protected virtual async Task<string> ScrapePageAsync(PageType pageType, string url, CancellationToken cancellationToken)
    {
        Logger.LogDebug("Scraping {PageType} from {Url}", pageType, url);

        using var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new CookieContainer()
        };

        // Add cookies to the container
        foreach (var cookie in CookieFile!.Cookies)
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
    /// Executes the main scraping logic. Must be implemented by derived classes.
    /// </summary>
    /// <param name="batch">The staging batch to populate with scraped data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected abstract Task ExecuteScrapingAsync(StagingBatch batch, CancellationToken cancellationToken);

    /// <summary>
    /// Extracts order links from the orders list page. Must be implemented by derived classes.
    /// </summary>
    protected abstract IEnumerable<string> ExtractOrderLinks(string ordersListHtml);

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
