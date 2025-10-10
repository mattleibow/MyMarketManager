using MyMarketManager.Data.Services.Scraping;
using Xunit;

namespace MyMarketManager.Data.Tests.Services;

public class ScraperConfigurationTests
{
    [Fact]
    public void ScraperConfiguration_CanBeCreated()
    {
        // Arrange & Act
        var config = new ScraperConfiguration
        {
            SupplierName = "Test Supplier",
            Domain = "example.com",
            OrdersListUrl = "https://example.com/orders",
            OrderDetailUrlPattern = "https://example.com/orders/{orderId}"
        };

        // Assert
        Assert.Equal("Test Supplier", config.SupplierName);
        Assert.Equal("example.com", config.Domain);
        Assert.Equal("https://example.com/orders", config.OrdersListUrl);
        Assert.Empty(config.AdditionalHeaders);
    }

    [Fact]
    public void ScraperConfiguration_HasDefaultValues()
    {
        // Arrange & Act
        var config = new ScraperConfiguration();

        // Assert
        Assert.Equal(string.Empty, config.SupplierName);
        Assert.Equal(1000, config.RequestDelayMs);
        Assert.Equal(1, config.MaxConcurrentRequests);
        Assert.Equal(30, config.RequestTimeoutSeconds);
        Assert.False(config.RequiresHeadlessBrowser);
        Assert.NotEmpty(config.UserAgent);
    }

    [Fact]
    public void ScraperConfiguration_CanAddHeaders()
    {
        // Arrange
        var config = new ScraperConfiguration();

        // Act
        config.AdditionalHeaders["accept"] = "text/html";
        config.AdditionalHeaders["accept-language"] = "en-US";

        // Assert
        Assert.Equal(2, config.AdditionalHeaders.Count);
        Assert.Equal("text/html", config.AdditionalHeaders["accept"]);
    }

    [Fact]
    public void ScraperConfiguration_UrlPatternsCanContainPlaceholders()
    {
        // Arrange
        var config = new ScraperConfiguration
        {
            OrderDetailUrlPattern = "https://example.com/order/{orderId}",
            ProductPageUrlPattern = "https://example.com/product/{productId}"
        };

        // Act
        var orderUrl = config.OrderDetailUrlPattern.Replace("{orderId}", "12345");
        var productUrl = config.ProductPageUrlPattern.Replace("{productId}", "ABC123");

        // Assert
        Assert.Equal("https://example.com/order/12345", orderUrl);
        Assert.Equal("https://example.com/product/ABC123", productUrl);
    }
}
