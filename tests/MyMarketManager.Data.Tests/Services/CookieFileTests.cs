using System.Text.Json;
using MyMarketManager.Data.Services.Scraping;
using Xunit;

namespace MyMarketManager.Data.Tests.Services;

public class CookieFileTests
{
    [Fact]
    public void CookieFile_CanBeCreated()
    {
        // Arrange & Act
        var cookieFile = new CookieFile
        {
            SupplierId = Guid.NewGuid(),
            Domain = "shein.com",
            CapturedAt = DateTimeOffset.UtcNow
        };

        // Assert
        Assert.NotEqual(Guid.Empty, cookieFile.Id);
        Assert.Equal("shein.com", cookieFile.Domain);
        Assert.Empty(cookieFile.Cookies);
        Assert.Empty(cookieFile.Metadata);
    }

    [Fact]
    public void CookieFile_CanAddCookies()
    {
        // Arrange
        var cookieFile = new CookieFile
        {
            SupplierId = Guid.NewGuid(),
            Domain = "shein.com",
            CapturedAt = DateTimeOffset.UtcNow
        };

        var cookie = new CookieData
        {
            Name = "session_id",
            Value = "abc123",
            Domain = ".shein.com",
            Path = "/",
            Secure = true,
            HttpOnly = true
        };

        // Act
        cookieFile.Cookies.Add(cookie);

        // Assert
        Assert.Single(cookieFile.Cookies);
        Assert.Equal("session_id", cookieFile.Cookies[0].Name);
        Assert.Equal("abc123", cookieFile.Cookies[0].Value);
    }

    [Fact]
    public void CookieFile_CanSerializeToJson()
    {
        // Arrange
        var cookieFile = new CookieFile
        {
            Id = Guid.NewGuid(),
            SupplierId = Guid.NewGuid(),
            Domain = "shein.com",
            CapturedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            Cookies = new List<CookieData>
            {
                new CookieData
                {
                    Name = "auth_token",
                    Value = "xyz789",
                    Domain = ".shein.com",
                    Path = "/",
                    Secure = true,
                    HttpOnly = true,
                    SameSite = "Lax"
                }
            },
            Metadata = new Dictionary<string, string>
            {
                { "user_agent", "Mozilla/5.0" },
                { "platform", "web" }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(cookieFile, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        // Assert
        Assert.NotEmpty(json);
        Assert.Contains("shein.com", json);
        Assert.Contains("auth_token", json);
        Assert.Contains("xyz789", json);
        Assert.Contains("user_agent", json);
    }

    [Fact]
    public void CookieFile_CanDeserializeFromJson()
    {
        // Arrange
        var json = @"{
            ""Id"": ""12345678-1234-1234-1234-123456789012"",
            ""SupplierId"": ""87654321-4321-4321-4321-210987654321"",
            ""Domain"": ""shein.com"",
            ""CapturedAt"": ""2025-10-10T00:00:00Z"",
            ""ExpiresAt"": ""2025-10-17T00:00:00Z"",
            ""Cookies"": [
                {
                    ""name"": ""session"",
                    ""value"": ""abc123"",
                    ""domain"": "".shein.com"",
                    ""path"": ""/"",
                    ""secure"": true,
                    ""httpOnly"": true,
                    ""sameSite"": ""Strict""
                }
            ],
            ""Metadata"": {
                ""browser"": ""Chrome""
            }
        }";

        // Act
        var cookieFile = JsonSerializer.Deserialize<CookieFile>(json);

        // Assert
        Assert.NotNull(cookieFile);
        Assert.Equal("shein.com", cookieFile.Domain);
        Assert.Single(cookieFile.Cookies);
        Assert.Equal("session", cookieFile.Cookies[0].Name);
        Assert.Equal("abc123", cookieFile.Cookies[0].Value);
        Assert.True(cookieFile.Cookies[0].Secure);
        Assert.Single(cookieFile.Metadata);
        Assert.Equal("Chrome", cookieFile.Metadata["browser"]);
    }

    [Fact]
    public void CookieData_HasCorrectJsonPropertyNames()
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
        var json = JsonSerializer.Serialize(cookie);

        // Assert - verify JSON property names match the JsonPropertyName attributes
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
