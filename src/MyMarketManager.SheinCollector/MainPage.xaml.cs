using System.Text.Json;

namespace MyMarketManager.SheinCollector;

public partial class MainPage : ContentPage
{
    private readonly CookieService _cookieService;

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

            // Get cookies from WebView
            var cookies = await _cookieService.GetCookiesFromWebView(SheinWebView);

            // Save cookies to JSON
            var cookiesFilePath = await _cookieService.SaveCookiesToFile(cookies);
            StatusLabel.Text = $"Cookies saved to: {cookiesFilePath}";

            // Send HTTP request with cookies
            StatusLabel.Text = "Sending request to orders endpoint...";
            var response = await _cookieService.FetchOrdersWithCookies(cookies);

            if (response.Contains("gbRawData"))
            {
                StatusLabel.Text = "✓ Success! Found 'gbRawData' in response.";
                StatusLabel.TextColor = Colors.Green;
            }
            else
            {
                StatusLabel.Text = "⚠ Warning: 'gbRawData' not found in response.";
                StatusLabel.TextColor = Colors.Orange;
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

    private void OnReloadClicked(object sender, EventArgs e)
    {
        SheinWebView.Source = "https://shein.com/user/auth/login";
        StatusLabel.Text = "Reloading login page...";
        StatusLabel.TextColor = Colors.Black;
        ResultLabel.Text = string.Empty;
    }
}
