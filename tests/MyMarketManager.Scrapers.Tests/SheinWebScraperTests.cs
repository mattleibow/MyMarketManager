using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
using MyMarketManager.Tests.Shared;
using NSubstitute;

namespace MyMarketManager.Scrapers.Tests;

[Trait(TestCategories.Key, TestCategories.Values.Database)]
public class SheinWebScraperTests(ITestOutputHelper outputHelper) : WebScraperTests<SheinWebScraper>(outputHelper)
{
    private CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public void CanBeCreated()
    {
        new SheinWebScraper(Context, ScraperLogger, ScraperConfig);
    }

    [Fact]
    public async Task ParsesOrdersListCorrectly()
    {
        // Arrange
        var scraper = new SheinWebScraper(Context, ScraperLogger, ScraperConfig);
        var ordersListHtml = LoadHtmlFixture("shein_order_list.html");

        // Act
        var orders = await scraper.ParseOrdersListAsync(ordersListHtml, Cancel).ToListAsync(Cancel);

        // Assert
        Assert.Equal(2, orders.Count);
        Assert.Contains(orders, o => o.ContainsKey("orderId") && o["orderId"] == "TEST001ORDER001");
    }

    [Theory]
    [InlineData("TEST001ORDER001")]
    public async Task ParsesOrderDetailsCorrectly(string orderId)
    {
        // Arrange
        var scraper = new SheinWebScraper(Context, ScraperLogger, ScraperConfig);
        var orderDetailsHtml = LoadHtmlFixture($"shein_order_detail_{orderId.ToLower()}.html");

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

        var scraper = MockScraper(new()
        {
            ["https://shein.com/user/orders/list"] = LoadHtmlFixture("shein_order_list.html"),
            ["https://shein.com/user/orders/detail/TEST001ORDER001"] = LoadHtmlFixture("shein_order_detail_test001order001.html")
        });

        // Act - Using mock scraper with cached HTML fixtures
        await scraper.StartScrapingAsync(supplier.Id, null, TestContext.Current.CancellationToken);

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

    private SheinWebScraper MockScraper(Dictionary<string, string?>? customResponses = null)
    {
        var scraper = Substitute.For<SheinWebScraper>(Context, ScraperLogger, ScraperConfig);
        
        MockResponses(scraper, customResponses);

        return scraper;
    }
}
