using Microsoft.Extensions.Logging;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
using MyMarketManager.Scrapers.Core;
using MyMarketManager.Scrapers.Shein;
using MyMarketManager.Tests.Shared;
using NSubstitute;

namespace MyMarketManager.Scrapers.Tests.Shein;

[Trait(TestCategories.Key, TestCategories.Values.Database)]
public class SheinWebScraperTests(ITestOutputHelper outputHelper) : WebScraperTests<SheinWebScraper>(outputHelper)
{
    private CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public void CanBeCreated()
    {
        var sessionFactory = CreateMockSessionFactory(new());
        new SheinWebScraper(Context, ScraperLogger, ScraperConfig, sessionFactory);
    }

    [Fact]
    public async Task ParsesOrdersListCorrectly()
    {
        // Arrange
        var sessionFactory = CreateMockSessionFactory(new());
        var scraper = new SheinWebScraper(Context, ScraperLogger, ScraperConfig, sessionFactory);
        var ordersListHtml = LoadHtmlFixture("shein_order_list.html");

        // Act
        var orders = await scraper.ParseOrdersListAsync(ordersListHtml, Cancel).ToListAsync(Cancel);

        // Assert
        Assert.Single(orders);
        Assert.Contains(orders, o => o.ContainsKey("orderNumber") && o["orderNumber"] == "TEST001ORDER001");
    }

    [Theory]
    [InlineData("TEST001ORDER001")]
    public async Task ParsesOrderDetailsCorrectly(string orderId)
    {
        // Arrange
        var sessionFactory = CreateMockSessionFactory(new());
        var scraper = new SheinWebScraper(Context, ScraperLogger, ScraperConfig, sessionFactory);
        var orderDetailsHtml = LoadHtmlFixture($"shein_order_detail_{orderId}.html");

        // Act
        var order = await scraper.ParseOrderDetailsAsync(orderDetailsHtml, new(), Cancel);

        // Assert
        Assert.NotNull(order);
    }

    [Fact]
    public async Task CreatesSessionAndBatch()
    {
        // Arrange
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = "Shein"
        };
        Context.Suppliers.Add(supplier);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mockResponses = new Dictionary<string, string?>
        {
            ["https://shein.com/user/orders/list"] = LoadHtmlFixture("shein_order_list.html"),
            ["https://shein.com/user/orders/detail/TEST001ORDER001"] = LoadHtmlFixture("shein_order_detail_TEST001ORDER001.html")
        };
        var sessionFactory = CreateMockSessionFactory(mockResponses);
        var scraper = new SheinWebScraper(Context, ScraperLogger, ScraperConfig, sessionFactory);

        // Create a staging batch for processing
        var batch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            SupplierId = supplier.Id,
            BatchType = StagingBatchType.WebScrape,
            Status = ProcessingStatus.Started,
            FileContents = "{}", // Empty JSON for mock cookies
            CreatedAt = DateTimeOffset.UtcNow
        };
        Context.StagingBatches.Add(batch);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - Using mock scraper with cached HTML fixtures
        await scraper.ProcessBatchAsync(batch, TestContext.Current.CancellationToken);

        // Assert - Verify batch was processed
        var batches = Context.StagingBatches.ToList();
        Assert.Single(batches);
        Assert.Equal(supplier.Id, batches[0].SupplierId);
        Assert.Equal(StagingBatchType.WebScrape, batches[0].BatchType);

        // Verify orders were scraped
        var orders = Context.StagingPurchaseOrders.ToList();
        Assert.Single(orders); // Should have scraped TEST001ORDER001

        // Order should be completed successfully now
        var order = orders[0];
        Assert.Equal(ProcessingStatus.Completed, order.Status);
        Assert.Equal("TEST001ORDER001", order.SupplierReference);
        Assert.NotNull(order.RawData);
        Assert.NotEmpty(order.Items);
    }

    public static bool HasIntegrationCookies =>
        FixtureFileExists("cookies.shein.json");

    [Fact(Skip = "This requires real cookies stored at Fixtures/cookies.shein.json", SkipUnless = nameof(HasIntegrationCookies))]
    public async Task Integration()
    {
        // Arrange
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = "Shein"
        };
        Context.Suppliers.Add(supplier);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var scraper = new SheinWebScraper(
            Context,
            ScraperLogger,
            ScraperConfig,
            new WebScraperSessionFactory(Substitute.For<ILogger<WebScraperSessionFactory>>(), ScraperConfig));

        // Create a staging batch for processing
        var batch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            SupplierId = supplier.Id,
            BatchType = StagingBatchType.WebScrape,
            Status = ProcessingStatus.Started,
            FileContents = LoadFixture("cookies.shein.json"),
            CreatedAt = DateTimeOffset.UtcNow
        };
        Context.StagingBatches.Add(batch);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await scraper.ProcessBatchAsync(batch, TestContext.Current.CancellationToken);

        // Assert

        // Verify batch was processed
        var batches = Context.StagingBatches.ToList();
        Assert.NotEmpty(batches);

        // Verify orders were scraped
        var orders = Context.StagingPurchaseOrders.ToList();
        Assert.NotEmpty(orders);
    }
}
