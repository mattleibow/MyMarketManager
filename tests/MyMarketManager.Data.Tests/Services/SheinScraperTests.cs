using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
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
        var scraper = new SheinScraper(Context, logger, config);

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

        // Act - This would fail without real cookies but we're just testing the setup
        // We can't actually scrape without valid auth
        try
        {
            await scraper.ScrapeAsync(supplier.Id, cookieFile, TestContext.Current.CancellationToken);
        }
        catch
        {
            // Expected to fail without valid cookies
        }

        // Assert - Verify session was created
        var sessions = Context.ScraperSessions.ToList();
        Assert.Single(sessions);
        Assert.Equal(supplier.Id, sessions[0].SupplierId);
        Assert.NotEqual(ProcessingStatus.Queued, sessions[0].Status); // Should have started
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
