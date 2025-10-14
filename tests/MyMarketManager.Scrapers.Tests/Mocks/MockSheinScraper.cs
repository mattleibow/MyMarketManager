using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMarketManager.Data;
using MyMarketManager.Scrapers.Tests.Helpers;
using MyMarketManager.Scrapers;
using MyMarketManager.Scrapers.Core;

namespace MyMarketManager.Scrapers.Tests.Mocks;

/// <summary>
/// Mock SheinScraper that uses cached HTML fixtures instead of making real HTTP requests.
/// </summary>
public class MockSheinScraper : SheinScraper
{
    private readonly HttpClient _mockHttpClient;

    public MockSheinScraper(
        MyMarketManagerDbContext context,
        ILogger<SheinScraper> logger,
        IOptions<ScraperConfiguration> configuration,
        HttpClient mockHttpClient)
        : base(context, logger, configuration)
    {
        _mockHttpClient = mockHttpClient ?? throw new ArgumentNullException(nameof(mockHttpClient));
    }

    /// <summary>
    /// Creates a mock SheinScraper with predefined HTML fixtures.
    /// </summary>
    public static MockSheinScraper CreateWithStandardFixtures(
        MyMarketManagerDbContext context,
        ILogger<SheinScraper> logger,
        IOptions<ScraperConfiguration> configuration)
    {
        var urlToFixtureMap = new Dictionary<string, string>
        {
            ["https://shein.com/user/account"] = "shein_account_page.html",
            ["https://shein.com/user/orders/list"] = "shein_orders_list.html",
            ["order_id=ORDER123"] = "shein_order_detail_ORDER123.html",
            ["order_id=ORDER456"] = "shein_order_detail_ORDER456.html"
        };

        var handler = new MockHttpMessageHandler(urlToFixtureMap);
        var mockHttpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        return new MockSheinScraper(context, logger, configuration, mockHttpClient);
    }

    protected override async Task<string> FetchPageAsync(string url, CookieFile cookies, CancellationToken cancellationToken)
    {
        // Don't dispose the mock client - reuse it for all requests
        var response = await _mockHttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <summary>
    /// Public wrapper for testing ParseOrdersListAsync.
    /// </summary>
    public new IEnumerable<WebScraperOrderSummary> ParseOrdersListAsync(string ordersListHtml)
    {
        return base.ParseOrdersListAsync(ordersListHtml);
    }
}
