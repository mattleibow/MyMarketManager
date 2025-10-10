using System.Net;
using System.Text.Json;

namespace MyMarketManager.SheinCollector;

public class CookieService
{
    private const string CookiesFileName = "shein_cookies.json";

    public async Task<List<CookieData>> GetCookiesFromWebView(WebView webView)
    {
        var cookies = new List<CookieData>();

        // Get cookies using platform-specific implementation
#if ANDROID
        cookies = await GetCookiesAndroid();
#elif IOS || MACCATALYST
        cookies = await GetCookiesIos();
#elif WINDOWS
        cookies = await GetCookiesWindows();
#endif

        return cookies;
    }

#if ANDROID
    private async Task<List<CookieData>> GetCookiesAndroid()
    {
        var cookies = new List<CookieData>();
        
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var cookieManager = Android.Webkit.CookieManager.Instance;
            if (cookieManager != null)
            {
                var cookieString = cookieManager.GetCookie("https://shein.com");
                if (!string.IsNullOrEmpty(cookieString))
                {
                    var cookiePairs = cookieString.Split(';');
                    foreach (var pair in cookiePairs)
                    {
                        var trimmedPair = pair.Trim();
                        var equalIndex = trimmedPair.IndexOf('=');
                        if (equalIndex > 0)
                        {
                            var name = trimmedPair.Substring(0, equalIndex);
                            var value = trimmedPair.Substring(equalIndex + 1);
                            cookies.Add(new CookieData
                            {
                                Name = name,
                                Value = value,
                                Domain = ".shein.com"
                            });
                        }
                    }
                }
            }
        });

        return cookies;
    }
#endif

#if IOS || MACCATALYST
    private async Task<List<CookieData>> GetCookiesIos()
    {
        var cookies = new List<CookieData>();
        
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var cookieStore = WebKit.WKWebsiteDataStore.DefaultDataStore.HttpCookieStore;
            var allCookies = await cookieStore.GetAllCookiesAsync();
            
            foreach (var cookie in allCookies)
            {
                if (cookie.Domain.Contains("shein.com"))
                {
                    cookies.Add(new CookieData
                    {
                        Name = cookie.Name,
                        Value = cookie.Value,
                        Domain = cookie.Domain,
                        Path = cookie.Path,
                        Secure = cookie.IsSecure,
                        HttpOnly = cookie.IsHttpOnly
                    });
                }
            }
        });

        return cookies;
    }
#endif

#if WINDOWS
    private async Task<List<CookieData>> GetCookiesWindows()
    {
        var cookies = new List<CookieData>();
        
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var cookieManager = Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString();
            // Note: Windows WebView2 cookie access requires additional setup
            // For now, return empty list as this is a simplified implementation
            // In production, you'd need to properly access WebView2 cookies
        });

        return cookies;
    }
#endif

    public async Task<string> SaveCookiesToFile(List<CookieData> cookies)
    {
        var json = JsonSerializer.Serialize(cookies, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var filePath = Path.Combine(FileSystem.AppDataDirectory, CookiesFileName);
        await File.WriteAllTextAsync(filePath, json);

        return filePath;
    }

    public async Task<string> FetchOrdersWithCookies(List<CookieData> cookies)
    {
        using var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new CookieContainer()
        };

        // Add cookies to the container
        foreach (var cookie in cookies)
        {
            try
            {
                handler.CookieContainer.Add(new Uri("https://shein.com"), new Cookie
                {
                    Name = cookie.Name,
                    Value = cookie.Value,
                    Domain = cookie.Domain ?? ".shein.com",
                    Path = cookie.Path ?? "/",
                    Secure = cookie.Secure,
                    HttpOnly = cookie.HttpOnly
                });
            }
            catch (Exception ex)
            {
                // Skip invalid cookies
                Console.WriteLine($"Failed to add cookie {cookie.Name}: {ex.Message}");
            }
        }

        using var client = new HttpClient(handler);

        // Add required headers
        client.DefaultRequestHeaders.Add("accept", "text/html");
        client.DefaultRequestHeaders.Add("accept-language", "en-US");
        client.DefaultRequestHeaders.Add("cache-control", "no-cache");
        client.DefaultRequestHeaders.Add("upgrade-insecure-requests", "1");
        client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36 Edg/141.0.0.0");

        var response = await client.GetAsync("https://shein.com/user/orders/list");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}

public class CookieData
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public string? Path { get; set; }
    public bool Secure { get; set; }
    public bool HttpOnly { get; set; }
}
