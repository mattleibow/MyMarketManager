using System.Text.Json;
using MyMarketManager.Scrapers.Core;

namespace MyMarketManager.SheinCollector;

public partial class MainPage : ContentPage
{
    private readonly CookieService _cookieService;
    private List<CookieData>? _lastCollectedCookies;

    public MainPage(CookieService cookieService)
    {
        InitializeComponent();
        _cookieService = cookieService;
    }

    private async void OnDoneClicked(object sender, EventArgs e)
    {
        try
        {
            StatusLabel.Text = "Collecting cookies...";
            DoneButton.IsEnabled = false;
            CopyJsonButton.IsEnabled = false;

            // Get cookies from WebView
            var cookies = await _cookieService.GetCookiesFromWebView(SheinWebView);
            _lastCollectedCookies = cookies;

            // Save cookies to JSON
            var cookiesFilePath = await _cookieService.SaveCookiesToFile(cookies);
            StatusLabel.Text = $"Cookies saved to: {cookiesFilePath}";

            // Send HTTP request with cookies
            StatusLabel.Text = "Sending request to orders endpoint...";
            var response = await _cookieService.FetchOrdersWithCookies(cookies);

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
        try
        {
            if (_lastCollectedCookies == null || _lastCollectedCookies.Count == 0)
            {
                await DisplayAlertAsync("No Cookies", "No cookies have been collected yet. Click 'Done' first.", "OK");
                return;
            }

            var json = _cookieService.CreateCookieFileJson(_lastCollectedCookies);
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
}
