using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
using MyMarketManager.Data.Services.Scraping;
using MyMarketManager.Tests.Shared;
using Xunit;

namespace MyMarketManager.Data.Tests.Services;

[Trait(TestCategories.Key, TestCategories.Values.Database)]
public class SheinScraperTests(ITestOutputHelper outputHelper) : SqliteTestBase(outputHelper)
{
    [Fact]
    public void SheinScraper_CanBeCreated()
    {
        // Arrange
        var logger = CreateLogger<SheinScraper>();
        var config = CreateConfiguration();

        // Act
        var scraper = new SheinScraper(Context, logger, config);

        // Assert
        Assert.NotNull(scraper);
    }

    [Fact]
    public async Task SheinScraper_ImplementsIWebScraperInterface()
    {
        // Arrange
        var logger = CreateLogger<SheinScraper>();
        var config = CreateConfiguration();

        // Act
        IWebScraper scraper = new SheinScraper(Context, logger, config);

        // Assert
        Assert.NotNull(scraper);

        // Initialize with a cookie file
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = "Shein"
        };
        Context.Suppliers.Add(supplier);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var cookieFile = new CookieFile
        {
            SupplierId = supplier.Id,
            SupplierName = "Shein",
            Domain = "shein.com",
            CapturedAt = DateTimeOffset.UtcNow,
            Cookies = new List<CookieData>
            {
                new CookieData
                {
                    Name = "session",
                    Value = "test123",
                    Domain = ".shein.com"
                }
            }
        };

        // This will create a scraper session
        var baseScraper = (WebScraperBase)scraper;
        await baseScraper.InitializeAsync(cookieFile, TestContext.Current.CancellationToken);

        // Verify session was created
        var sessions = Context.ScraperSessions.ToList();
        Assert.Single(sessions);
        Assert.Equal(supplier.Id, sessions[0].SupplierId);
        Assert.Equal(ProcessingStatus.Queued, sessions[0].Status);
    }

    private ILogger<T> CreateLogger<T>()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        return loggerFactory.CreateLogger<T>();
    }

    private IOptions<ScraperConfiguration> CreateConfiguration()
    {
        var config = new ScraperConfiguration
        {
            SupplierName = "Shein",
            Domain = "shein.com",
            OrdersListUrlTemplate = "https://shein.com/user/orders/list",
            OrderDetailUrlTemplate = "https://shein.com/user/orders/detail?order_id={orderId}",
            ProductPageUrlTemplate = "https://shein.com/product/{productId}",
            AccountPageUrlTemplate = "https://shein.com/user/account",
            UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36 Edg/141.0.0.0",
            AdditionalHeaders = new Dictionary<string, string>
            {
                { "accept", "text/html" },
                { "accept-language", "en-US" },
                { "cache-control", "no-cache" },
                { "upgrade-insecure-requests", "1" }
            },
            RequestDelay = TimeSpan.FromSeconds(2),
            MaxConcurrentRequests = 1,
            RequestTimeout = TimeSpan.FromSeconds(30),
            RequiresHeadlessBrowser = false,
            Notes = "Shein orders are listed at /user/orders/list. Check for 'gbRawData' in response to verify successful authentication."
        };

        return Options.Create(config);
    }
}
