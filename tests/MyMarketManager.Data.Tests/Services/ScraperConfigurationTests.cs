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
            OrdersListUrlTemplate = "https://example.com/orders",
            OrderDetailUrlTemplate = "https://example.com/orders/{orderId}"
        };

        // Assert
        Assert.Equal("Test Supplier", config.SupplierName);
        Assert.Equal("example.com", config.Domain);
        Assert.Equal("https://example.com/orders", config.OrdersListUrlTemplate);
        Assert.Empty(config.AdditionalHeaders);
    }

    [Fact]
    public void ScraperConfiguration_HasDefaultValues()
    {
        // Arrange & Act
        var config = new ScraperConfiguration();

        // Assert
        Assert.Equal(string.Empty, config.SupplierName);
        Assert.Equal(TimeSpan.FromSeconds(1), config.RequestDelay);
        Assert.Equal(1, config.MaxConcurrentRequests);
        Assert.Equal(TimeSpan.FromSeconds(30), config.RequestTimeout);
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
    public void ScraperConfiguration_UrlTemplatesCanContainPlaceholders()
    {
        // Arrange
        var config = new ScraperConfiguration
        {
            OrderDetailUrlTemplate = "https://example.com/order/{orderId}",
            ProductPageUrlTemplate = "https://example.com/product/{productId}"
        };

        // Act
        var orderUrl = config.OrderDetailUrlTemplate.Replace("{orderId}", "12345");
        var productUrl = config.ProductPageUrlTemplate.Replace("{productId}", "ABC123");

        // Assert
        Assert.Equal("https://example.com/order/12345", orderUrl);
        Assert.Equal("https://example.com/product/ABC123", productUrl);
    }
}
