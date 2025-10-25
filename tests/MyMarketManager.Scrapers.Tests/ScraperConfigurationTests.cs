namespace MyMarketManager.Scrapers.Tests;

public class ScraperConfigurationTests
{
    [Fact]
    public void CanBeCreated()
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
        Assert.NotEmpty(config.AdditionalHeaders); // Has default headers
        Assert.Equal(4, config.AdditionalHeaders.Count); // Default header count
    }

    [Fact]
    public void HasDefaultValues()
    {
        // Arrange & Act
        var config = new ScraperConfiguration();

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(2), config.RequestDelay);
        Assert.Equal(1, config.MaxConcurrentRequests);
        Assert.Equal(TimeSpan.FromSeconds(30), config.RequestTimeout);
        Assert.NotEmpty(config.UserAgent);
    }

    [Fact]
    public void CanAddHeaders()
    {
        // Arrange
        var config = new ScraperConfiguration();

        // Act - Add new headers that don't exist in defaults
        config.AdditionalHeaders["Custom-Header"] = "custom-value";
        config.AdditionalHeaders["Another-Header"] = "another-value";

        // Assert
        Assert.Equal(6, config.AdditionalHeaders.Count); // 4 defaults + 2 new
        Assert.Equal("custom-value", config.AdditionalHeaders["Custom-Header"]);
        Assert.Equal("another-value", config.AdditionalHeaders["Another-Header"]);
    }
}
