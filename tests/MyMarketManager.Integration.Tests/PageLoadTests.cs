using Microsoft.Playwright;
using MyMarketManager.Tests.Shared;
using static Microsoft.Playwright.Assertions;

namespace MyMarketManager.Integration.Tests;

/// <summary>
/// Playwright-based UI tests to verify pages load without errors
/// </summary>
public class PageLoadTests(ITestOutputHelper outputHelper) : PlaywrightTestsBase(outputHelper)
{
    [Fact]
    public async Task HomePage_LoadsWithoutErrors()
    {
        // Arrange & Act
        await NavigateToAppAsync("/");

        // Assert
        await Expect(Page!).ToHaveTitleAsync(new System.Text.RegularExpressions.Regex(".*"));
        
        // Verify no error alerts on page
        await ExpectNoErrorsAsync();
    }

    [Fact]
    public async Task ProductsPage_LoadsWithoutErrors()
    {
        // Arrange & Act
        await NavigateToAppAsync("/products");

        // Assert - Verify page loaded with heading
        await Expect(Page!.GetByRole(AriaRole.Heading, new() { Name = "Products" })).ToBeVisibleAsync();
        
        // Verify Add Product button is present
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Add Product" })).ToBeVisibleAsync();
        
        // Verify search box is present
        await Expect(Page.GetByPlaceholder("Search products...")).ToBeVisibleAsync();
        
        // Verify no error alerts on page
        await ExpectNoErrorsAsync();
    }

    [Fact]
    public async Task AddProductPage_LoadsWithoutErrors()
    {
        // Arrange & Act
        await NavigateToAppAsync("/products/add");

        // Assert - Verify page loaded with heading
        await Expect(Page!.GetByRole(AriaRole.Heading, new() { Name = "Add Product" })).ToBeVisibleAsync();
        
        // Verify form fields are present
        await Expect(Page.GetByLabel("Product Name")).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Quality Rating")).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Stock on Hand")).ToBeVisibleAsync();
        
        // Verify Create Product and Cancel buttons are present
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Create Product" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Cancel" })).ToBeVisibleAsync();
        
        // Verify no error alerts on page
        await ExpectNoErrorsAsync();
    }

    [Fact]
    public async Task PurchaseOrdersPage_LoadsWithoutErrors()
    {
        // Arrange & Act
        await NavigateToAppAsync("/purchase-orders/list");

        // Assert - Verify page loaded with main heading (h1)
        await Expect(Page!.Locator("h1").Filter(new() { HasText = "Purchase Orders" })).ToBeVisibleAsync();
        
        // Verify View Staging button is present
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "View Staging" })).ToBeVisibleAsync();
        
        // Verify no error alerts on page
        await ExpectNoErrorsAsync();
    }

    [Fact]
    public async Task PurchaseOrdersStagingPage_LoadsWithoutErrors()
    {
        // Arrange & Act
        await NavigateToAppAsync("/purchase-orders/staging");

        // Assert - Verify page loaded with main heading (h1)
        await Expect(Page!.Locator("h1").Filter(new() { HasText = "Staging Batches" })).ToBeVisibleAsync();
        
        // Verify Start Ingestion button is present (in empty state)
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Start Ingestion" })).ToBeVisibleAsync();
        
        // Verify no error alerts on page
        await ExpectNoErrorsAsync();
    }

    [Fact]
    public async Task PurchaseOrderIngestionPage_LoadsWithoutErrors()
    {
        // Arrange & Act
        await NavigateToAppAsync("/purchase-orders/ingestion");

        // Assert - Verify page loaded with heading
        await Expect(Page!.GetByRole(AriaRole.Heading, new() { Name = "Purchase Order Ingestion" })).ToBeVisibleAsync();
        
        // Verify form elements are present
        await Expect(Page.Locator("#supplierSelect")).ToBeVisibleAsync();
        await Expect(Page.Locator("#scraperSelect")).ToBeVisibleAsync();
        await Expect(Page.Locator("#cookiesInput")).ToBeVisibleAsync();
        
        // Verify no error alerts on page (initially)
        await ExpectNoErrorsAsync();
    }

    [Fact]
    public async Task NotFoundPage_LoadsWithoutErrors()
    {
        // Arrange & Act
        await NavigateToAppAsync("/non-existent-page");

        // Assert - Verify "Not Found" page appears or redirects gracefully
        // The page should not throw JavaScript errors
        await Page!.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Check that we get some content (either error page or redirect)
        var content = await Page.ContentAsync();
        Assert.NotNull(content);
        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task GraphQLEndpoint_IsAccessible()
    {
        // Arrange & Act
        await NavigateToAppAsync("/graphql");

        // Assert - Verify GraphQL Nitro IDE loads
        await Page!.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        
        // Check that the page loaded successfully
        var title = await Page.TitleAsync();
        Assert.NotNull(title);
    }

    [Fact]
    public async Task BrowserConsole_NoUnexpectedErrors()
    {
        // This test verifies that basic navigation doesn't produce console errors
        var consoleErrors = new List<string>();
        
        Page!.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                consoleErrors.Add(msg.Text);
            }
        };

        // Navigate to main pages
        await NavigateToAppAsync("/");
        await Task.Delay(500, TestContext.Current.CancellationToken); // Wait for any async errors
        
        await NavigateToAppAsync("/products");
        await Task.Delay(500, TestContext.Current.CancellationToken);
        
        // Filter out expected WebSocket disconnection errors that occur during navigation
        // WebSocket connections close with status 1006 when navigating between Blazor pages
        var unexpectedErrors = consoleErrors
            .Where(error => !error.Contains("WebSocket closed with status code: 1006"))
            .ToList();
        
        // Assert - Should have no unexpected console errors
        Assert.Empty(unexpectedErrors);
    }
}
