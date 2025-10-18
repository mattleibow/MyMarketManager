using System.Text.Json;

namespace MyMarketManager.Scrapers.Core.Tests;

public class CookieDataTests
{
    [Fact]
    public void HasCorrectJsonPropertyNames()
    {
        // Arrange
        var cookie = new CookieData
        {
            Name = "test",
            Value = "value",
            Domain = ".example.com",
            Path = "/path",
            Secure = true,
            HttpOnly = false,
            Expires = DateTimeOffset.UtcNow.AddDays(1),
            SameSite = "None"
        };

        // Act
        var json = JsonSerializer.Serialize(cookie, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Assert - verify JSON uses camelCase
        Assert.Contains("\"name\":", json);
        Assert.Contains("\"value\":", json);
        Assert.Contains("\"domain\":", json);
        Assert.Contains("\"path\":", json);
        Assert.Contains("\"secure\":", json);
        Assert.Contains("\"httpOnly\":", json);
        Assert.Contains("\"expires\":", json);
        Assert.Contains("\"sameSite\":", json);
    }
}
