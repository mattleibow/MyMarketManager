/*using Microsoft.Playwright;
using MyMarketManager.Tests.Shared;
using static Microsoft.Playwright.Assertions;

namespace MyMarketManager.Integration.Tests;

/// <summary>
/// Playwright-based UI tests to verify pages load without errors
/// </summary>
[Trait(TestCategories.Key, TestCategories.Values.LongRunning)]
public class PageLoadTests(ITestOutputHelper outputHelper) : PlaywrightTestsBase(outputHelper)
{
    [Fact]
    public async Task HomePage_LoadsWithoutErrors()
    {
        // Arrange & Act
        await NavigateToAppAsync("/");

        // Assert
        await Expect(Page!).ToHaveTitleAsync(new System.Text.RegularExpressions.Regex(".*"));
        
        // Verify no JavaScript errors by checking console logs
        // The base class already logs console errors via Page.Console event
        
        // Check that the page loaded successfully (status code 200)
        var response = await Page.GotoAsync(WebAppHttpClient.BaseAddress!.ToString(), new() { WaitUntil = WaitUntilState.NetworkIdle });
        Assert.NotNull(response);
        Assert.True(response!.Ok, $"Page failed to load. Status: {response.Status}");
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
        var baseUrl = WebAppHttpClient.BaseAddress!.ToString().TrimEnd('/');
        await Page!.GotoAsync($"{baseUrl}/graphql", new() { WaitUntil = WaitUntilState.NetworkIdle });

        // Assert - Verify GraphQL Nitro IDE loads
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        
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
        
        // Assert - Should have no console errors
        Assert.Empty(consoleErrors);
    }
}
*/
