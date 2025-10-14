using MyMarketManager.Scrapers;
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
            UserAgent = "Mozilla/5.0",
            RequestDelay = TimeSpan.FromSeconds(2),
            MaxConcurrentRequests = 1
        };

        // Assert
        Assert.Equal("Mozilla/5.0", config.UserAgent);
        Assert.Equal(TimeSpan.FromSeconds(2), config.RequestDelay);
        Assert.Empty(config.AdditionalHeaders);
    }

    [Fact]
    public void ScraperConfiguration_HasDefaultValues()
    {
        // Arrange & Act
        var config = new ScraperConfiguration();

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(1), config.RequestDelay);
        Assert.Equal(1, config.MaxConcurrentRequests);
        Assert.Equal(TimeSpan.FromSeconds(30), config.RequestTimeout);
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
}
