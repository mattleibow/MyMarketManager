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
            
            
            Domain = "shein.com",
            CapturedAt = DateTimeOffset.UtcNow
        };

        // Assert
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
        cookieFile.Cookies.Add(cookie.Name, cookie);

        // Assert
        Assert.Single(cookieFile.Cookies);
        Assert.True(cookieFile.Cookies.ContainsKey("session_id"));
        Assert.Equal("abc123", cookieFile.Cookies["session_id"].Value);
    }

    [Fact]
    public void CookieFile_CanSerializeToJson()
    {
        // Arrange
        var cookieFile = new CookieFile
        {
            
            
            Domain = "shein.com",
            CapturedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            Cookies = new Dictionary<string, CookieData>
            {
                ["auth_token"] = new CookieData
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
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
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
            ""domain"": ""shein.com"",
            ""capturedAt"": ""2025-10-10T00:00:00Z"",
            ""expiresAt"": ""2025-10-17T00:00:00Z"",
            ""cookies"": {
                ""session"": {
                    ""name"": ""session"",
                    ""value"": ""abc123"",
                    ""domain"": "".shein.com"",
                    ""path"": ""/"",
                    ""secure"": true,
                    ""httpOnly"": true,
                    ""sameSite"": ""Strict""
                }
            },
            ""metadata"": {
                ""browser"": ""Chrome""
            }
        }";

        // Act
        var cookieFile = JsonSerializer.Deserialize<CookieFile>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Assert
        Assert.NotNull(cookieFile);
        Assert.Equal("shein.com", cookieFile.Domain);
        Assert.Single(cookieFile.Cookies);
        Assert.True(cookieFile.Cookies.ContainsKey("session"));
        Assert.Equal("abc123", cookieFile.Cookies["session"].Value);
        Assert.True(cookieFile.Cookies["session"].Secure);
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
