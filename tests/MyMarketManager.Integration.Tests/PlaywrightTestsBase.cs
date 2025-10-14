using Microsoft.Playwright;

namespace MyMarketManager.Integration.Tests;

/// <summary>
/// Base class for Playwright-based UI tests that test the full application stack
/// </summary>
public abstract class PlaywrightTestsBase(ITestOutputHelper outputHelper) : WebAppTestsBase(outputHelper)
{
    protected IPlaywright Playwright { get; private set; } = null!;
    protected IBrowser Browser { get; private set; } = null!;
    protected IBrowserContext Context { get; private set; } = null!;
    protected IPage Page { get; private set; } = null!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        // Initialize Playwright
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        
        // Launch browser in headless mode
        Browser = await Playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
        });

        // Create a new browser context
        Context = await Browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 720 },
            IgnoreHTTPSErrors = true, // For local development certificates
        });

        // Create a new page
        Page = await Context.NewPageAsync();

        // Enable console message logging for debugging
        Page.Console += (_, msg) =>
        {
            outputHelper.WriteLine($"Browser Console [{msg.Type}]: {msg.Text}");
        };

        // Log page errors
        Page.PageError += (_, exception) =>
        {
            outputHelper.WriteLine($"Browser Error: {exception}");
        };
    }

    public override async ValueTask DisposeAsync()
    {
        if (TestContext.Current.TestState?.Result == TestResult.Failed)
        {
            await CaptureScreenshotAsync();
        }

        if (Page is not null)
        {
            await Page.CloseAsync();
        }

        if (Context is not null)
        {
            await Context.CloseAsync();
        }

        if (Browser is not null)
        {
            await Browser.CloseAsync();
        }

        Playwright?.Dispose();

        await base.DisposeAsync();
    }

    protected async Task ExpectNoErrorsAsync()
    {
        // Check for error alerts on the page
        var errorAlerts = await Page!.Locator("[data-testid='error-alert']").AllAsync();
        Assert.Empty(errorAlerts);
    }

    /// <summary>
    /// Navigate to the web application with retry logic for transient network errors
    /// </summary>
    protected async Task NavigateToAppAsync(string path = "/")
    {
        var baseUrl = WebAppHttpClient.BaseAddress?.ToString().TrimEnd('/') ?? throw new InvalidOperationException("WebApp base URL not available");
        var url = $"{baseUrl}{path}";
        
        // Retry logic for transient network errors (e.g., ERR_NETWORK_CHANGED)
        const int maxRetries = 3;
        var retryDelay = TimeSpan.FromSeconds(1);
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await Page!.GotoAsync(url, new() 
                { 
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = 30000 // 30 second timeout
                });
                return; // Success
            }
            catch (PlaywrightException ex) when (attempt < maxRetries && 
                (ex.Message.Contains("ERR_NETWORK_CHANGED") || 
                 ex.Message.Contains("net::ERR_CONNECTION_REFUSED") ||
                 ex.Message.Contains("Timeout")))
            {
                outputHelper.WriteLine($"Navigation attempt {attempt} failed: {ex.Message}. Retrying...");
                await Task.Delay(retryDelay);
                retryDelay = TimeSpan.FromSeconds(retryDelay.TotalSeconds * 2); // Exponential backoff
            }
        }
    }
    
    protected async Task CaptureScreenshotAsync(string? name = null)
    {
        if (Page is null)
            return;

        name ??= TestContext.Current.Test!.TestDisplayName;
            
        try
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
            var fileName = $"{name}_{timestamp}.png";
            
            // Create test-results directory if it doesn't exist
            var testResultsDir = Path.Combine(Directory.GetCurrentDirectory(), "test-results", "screenshots");
            Directory.CreateDirectory(testResultsDir);
            
            var screenshotPath = Path.Combine(testResultsDir, fileName);
            await Page.ScreenshotAsync(new()
            {
                Path = screenshotPath,
                FullPage = true,
                Type = ScreenshotType.Png
            });

            var bytes = await File.ReadAllBytesAsync(screenshotPath);

            TestContext.Current.AddAttachment(name, bytes, "image/png");

            outputHelper.WriteLine($"Screenshot captured: {screenshotPath}");
        }
        catch (Exception ex)
        {
            outputHelper.WriteLine($"Failed to capture screenshot: {ex.Message}");
        }
    }
}
