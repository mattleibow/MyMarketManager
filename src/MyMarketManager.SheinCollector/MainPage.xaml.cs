using System.Net;
using System.Text.Json;
using MyMarketManager.Scrapers.Core;

namespace MyMarketManager.SheinCollector;

public partial class MainPage : ContentPage
{
    private const string CookiesFileName = "shein_cookies.json";
    private List<CookieData>? _lastCollectedCookies;

    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnDoneClicked(object sender, EventArgs e)
    {
        try
        {
            StatusLabel.Text = "Collecting cookies...";
            DoneButton.IsEnabled = false;
            CopyJsonButton.IsEnabled = false;

            // Get cookies from WebView
            var cookies = await GetCookiesFromWebView(SheinWebView);
            _lastCollectedCookies = cookies;

            // Send HTTP request with cookies
            StatusLabel.Text = "Sending request to orders endpoint...";
            var response = await FetchOrdersWithCookies(cookies);

            if (response.Contains("gbRawData"))
            {
                StatusLabel.Text = "✓ Success! Found 'gbRawData' in response. Click 'Copy JSON' to copy cookie data.";
                StatusLabel.TextColor = Colors.Green;
                CopyJsonButton.IsEnabled = true;
            }
            else
            {
                StatusLabel.Text = "⚠ Warning: 'gbRawData' not found in response. Click 'Copy JSON' to copy cookie data anyway.";
                StatusLabel.TextColor = Colors.Orange;
                CopyJsonButton.IsEnabled = true;
            }

            ResultLabel.Text = $"Response length: {response.Length} characters\nFirst 500 chars:\n{response.Substring(0, Math.Min(500, response.Length))}";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
            StatusLabel.TextColor = Colors.Red;
            ResultLabel.Text = ex.ToString();
        }
        finally
        {
            DoneButton.IsEnabled = true;
        }
    }

    private async void OnCopyJsonClicked(object sender, EventArgs e)
    {
        if (_lastCollectedCookies == null || _lastCollectedCookies.Count == 0)
        {
            await DisplayAlertAsync("No Cookies", "No cookies have been collected yet. Click 'Done' first.", "OK");
            return;
        }

        try
        {
            var json = CreateCookieFileJson(_lastCollectedCookies);
            await Clipboard.SetTextAsync(json);

            StatusLabel.Text = "✓ Cookie JSON copied to clipboard!";
            StatusLabel.TextColor = Colors.Green;

            await DisplayAlertAsync("Copied", "CookieFile JSON has been copied to clipboard. You can now paste it in your API request.", "OK");
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error copying to clipboard: {ex.Message}";
            StatusLabel.TextColor = Colors.Red;
            await DisplayAlertAsync("Error", $"Failed to copy to clipboard: {ex.Message}", "OK");
        }
    }

    private void OnReloadClicked(object sender, EventArgs e)
    {
        SheinWebView.Source = "https://shein.com/user/auth/login";
        StatusLabel.Text = "Reloading login page...";
        StatusLabel.TextColor = Colors.Black;
        ResultLabel.Text = string.Empty;
        _lastCollectedCookies = null;
        CopyJsonButton.IsEnabled = false;
    }

#if ANDROID
    private async Task<List<CookieData>> GetCookiesFromWebView(WebView webView)
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
    private async Task<List<CookieData>> GetCookiesFromWebView(WebView webView)
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
    private async Task<List<CookieData>> GetCookiesFromWebView(WebView webView)
    {
        var cookies = new List<CookieData>();
        
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                // Get the underlying WebView2 control from the MAUI WebView
                if (webView.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.WebView2 webView2)
                {
                    // Ensure CoreWebView2 is initialized
                    await webView2.EnsureCoreWebView2Async();
                    
                    if (webView2.CoreWebView2 != null)
                    {
                        var cookieManager = webView2.CoreWebView2.CookieManager;
                        var allCookies = await cookieManager.GetCookiesAsync("https://shein.com");
                        
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
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                System.Diagnostics.Debug.WriteLine($"Error getting cookies on Windows: {ex.Message}");
            }
        });

        return cookies;
    }
#endif

    private string CreateCookieFileJson(List<CookieData> cookies)
    {
        var cookieFile = new CookieFile
        {
            Domain = "shein.com",
            CapturedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7), // Assume cookies expire in 7 days
            Cookies = cookies.ToDictionary(c => c.Name, c => c),
            Metadata = new Dictionary<string, string>
            {
                { "source", "SheinCollector MAUI App" },
                { "platform", DeviceInfo.Platform.ToString() },
                { "version", AppInfo.VersionString }
            }
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(cookieFile, options);
    }

    private async Task<string> FetchOrdersWithCookies(List<CookieData> cookies)
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
