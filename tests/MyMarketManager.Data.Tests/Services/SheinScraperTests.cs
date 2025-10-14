using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
using MyMarketManager.Data.Tests.Helpers;
using MyMarketManager.Data.Tests.Mocks;
using MyMarketManager.Scrapers;
using MyMarketManager.Scrapers.Core;
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
    public async Task SheinScraper_CreatesSessionAndBatch()
    {
        // Arrange
        var logger = CreateLogger<SheinScraper>();
        var config = CreateConfiguration();
        var scraper = MockSheinScraper.CreateWithStandardFixtures(Context, logger, config);

        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = "Shein"
        };
        Context.Suppliers.Add(supplier);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var cookieFile = new CookieFile
        {
            Domain = "shein.com",
            CapturedAt = DateTimeOffset.UtcNow,
            Cookies = new Dictionary<string, CookieData>
            {
                ["session"] = new CookieData
                {
                    Name = "session",
                    Value = "test123",
                    Domain = ".shein.com"
                }
            }
        };

        // Act - Using mock scraper with cached HTML fixtures
        // Note: UpdateStagingOrderAsync is not implemented yet, so orders will fail
        await scraper.ScrapeAsync(supplier.Id, cookieFile, TestContext.Current.CancellationToken);

        // Assert - Verify session was created
        var sessions = Context.ScraperSessions.ToList();
        Assert.Single(sessions);
        Assert.Equal(supplier.Id, sessions[0].SupplierId);
        Assert.Equal(ProcessingStatus.Completed, sessions[0].Status);

        // Verify orders were scraped (even if they failed due to not implemented UpdateStagingOrderAsync)
        var orders = Context.StagingPurchaseOrders.ToList();
        Assert.Equal(2, orders.Count); // Should have scraped ORDER123 and ORDER456
        
        // Orders fail with NotImplementedException until UpdateStagingOrderAsync is implemented
        Assert.All(orders, order => 
        {
            Assert.Equal(ProcessingStatus.Failed, order.Status);
            Assert.Contains("not implemented", order.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void HtmlFixtures_AreEmbeddedCorrectly()
    {
        // Arrange & Act
        var accountPageHtml = HtmlFixtureLoader.Load("shein_account_page.html");
        var ordersListHtml = HtmlFixtureLoader.Load("shein_orders_list.html");
        var orderDetailHtml = HtmlFixtureLoader.Load("shein_order_detail_ORDER123.html");

        // Assert
        Assert.NotEmpty(accountPageHtml);
        Assert.Contains("gbRawData", accountPageHtml);

        Assert.NotEmpty(ordersListHtml);
        Assert.Contains("ORDER123", ordersListHtml);
        Assert.Contains("ORDER456", ordersListHtml);

        Assert.NotEmpty(orderDetailHtml);
        Assert.Contains("ORDER123", orderDetailHtml);
        Assert.Contains("gbRawData", orderDetailHtml);
    }

    [Fact]
    public void SheinScraper_ParsesOrdersListCorrectly()
    {
        // Arrange
        var logger = CreateLogger<SheinScraper>();
        var config = CreateConfiguration();
        var scraper = MockSheinScraper.CreateWithStandardFixtures(Context, logger, config);
        var ordersListHtml = HtmlFixtureLoader.Load("shein_orders_list.html");

        // Act
        var orders = scraper.ParseOrdersListAsync(ordersListHtml).ToList();

        // Assert
        Assert.Equal(2, orders.Count);
        Assert.Contains(orders, o => o.ContainsKey("orderId") && o["orderId"] == "ORDER123");
        Assert.Contains(orders, o => o.ContainsKey("orderId") && o["orderId"] == "ORDER456");
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
            UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36",
            AdditionalHeaders = new Dictionary<string, string>
            {
                { "accept", "text/html" },
                { "accept-language", "en-US" },
                { "cache-control", "no-cache" },
                { "upgrade-insecure-requests", "1" }
            },
            RequestDelay = TimeSpan.FromSeconds(2),
            MaxConcurrentRequests = 1,
            RequestTimeout = TimeSpan.FromSeconds(30)
        };

        return Options.Create(config);
    }
}
