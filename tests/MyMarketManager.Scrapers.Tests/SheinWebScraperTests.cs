using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
using MyMarketManager.Scrapers.Core;
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
        Assert.Single(orders);
        Assert.Contains(orders, o => o.ContainsKey("orderNumber") && o["orderNumber"] == "TEST001ORDER001");
    }

    [Theory]
    [InlineData("TEST001ORDER001")]
    public async Task ParsesOrderDetailsCorrectly(string orderId)
    {
        // Arrange
        var scraper = new SheinWebScraper(Context, ScraperLogger, ScraperConfig);
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

        var scraper = MockScraper(new()
        {
            ["https://shein.com/user/orders/list"] = LoadHtmlFixture("shein_order_list.html"),
            ["https://shein.com/user/orders/detail/TEST001ORDER001"] = LoadHtmlFixture("shein_order_detail_TEST001ORDER001.html")
        });

        // Act - Using mock scraper with cached HTML fixtures
        await scraper.StartScrapingAsync(supplier.Id, null, TestContext.Current.CancellationToken);

        // Assert - Verify batch was created
        var batches = Context.StagingBatches.ToList();
        Assert.Single(batches);
        Assert.Equal(supplier.Id, batches[0].SupplierId);
        Assert.Equal(ProcessingStatus.Completed, batches[0].Status);
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

    private SheinWebScraper MockScraper(Dictionary<string, string?>? customResponses = null)
    {
        var scraper = Substitute.ForPartsOf<SheinWebScraper>(Context, ScraperLogger, ScraperConfig);
        
        MockResponses(scraper, customResponses);

        return scraper;
    }
}
