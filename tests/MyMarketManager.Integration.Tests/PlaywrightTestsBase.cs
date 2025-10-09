using Microsoft.Playwright;

namespace MyMarketManager.Integration.Tests;

/// <summary>
/// Base class for Playwright-based UI tests that test the full application stack
/// </summary>
public abstract class PlaywrightTestsBase(ITestOutputHelper outputHelper) : WebAppTestsBase(outputHelper)
{
    protected IPlaywright? Playwright { get; private set; }
    protected IBrowser? Browser { get; private set; }
    protected IBrowserContext? Context { get; private set; }
    protected IPage? Page { get; private set; }

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

    /// <summary>
    /// Navigate to the web application
    /// </summary>
    protected async Task NavigateToAppAsync(string path = "/")
    {
        var baseUrl = WebAppHttpClient.BaseAddress?.ToString().TrimEnd('/') ?? throw new InvalidOperationException("WebApp base URL not available");
        await Page!.GotoAsync($"{baseUrl}{path}", new() { WaitUntil = WaitUntilState.NetworkIdle });
    }
}
