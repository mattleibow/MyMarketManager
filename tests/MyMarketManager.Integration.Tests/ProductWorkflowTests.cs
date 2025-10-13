using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace MyMarketManager.Integration.Tests;

/// <summary>
/// Playwright-based UI tests for end-to-end product workflows
/// </summary>
public class ProductWorkflowTests(ITestOutputHelper outputHelper) : PlaywrightTestsBase(outputHelper)
{
    [Fact]
    public async Task CreateProduct_WorkflowCompletes_Successfully()
    {
        // Arrange - Navigate to products page
        await NavigateToAppAsync("/products");
        
        // Verify no errors on products page
        var errorAlerts = await Page!.Locator("[data-testid='error-alert']").AllAsync();
        Assert.Empty(errorAlerts);

        // Act - Click Add Product button
        await Page.GetByRole(AriaRole.Button, new() { Name = "Add Product" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Verify we're on the add product page
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Add Product" })).ToBeVisibleAsync();
        
        // Verify no errors on add page
        errorAlerts = await Page.Locator("[data-testid='error-alert']").AllAsync();
        Assert.Empty(errorAlerts);

        // Fill in the form with test data
        var productName = $"Test Product {Guid.NewGuid().ToString()[..8]}";
        await Page.GetByLabel("Product Name").FillAsync(productName);
        await Page.GetByLabel("SKU").FillAsync($"SKU-{Guid.NewGuid().ToString()[..6]}");
        await Page.GetByLabel("Description").FillAsync("Test product description");
        await Page.GetByLabel("Quality Rating").SelectOptionAsync("Good");
        await Page.GetByLabel("Stock on Hand").FillAsync("10");
        await Page.GetByLabel("Notes").FillAsync("Test notes");

        // Submit the form
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create Product" }).ClickAsync();
        
        // Wait for navigation back to products page
        await Page.WaitForURLAsync("**/products", new() { WaitUntil = WaitUntilState.NetworkIdle });
        
        // Assert - Verify we're back on products page
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Products" })).ToBeVisibleAsync();
        
        // Verify no error alerts
        errorAlerts = await Page.Locator("[data-testid='error-alert']").AllAsync();
        Assert.Empty(errorAlerts);

        // Verify the new product is visible in the list
        await Expect(Page.Locator($"text={productName}")).ToBeVisibleAsync(new() { Timeout = 5000 });
    }

    [Fact]
    public async Task EditProduct_WorkflowCompletes_Successfully()
    {
        // Arrange - First create a product to edit
        await NavigateToAppAsync("/products");
        
        // Wait for products to load
        await Page!.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(1000); // Give time for data to load
        
        // Check if there are any products, if not create one first
        var editButtons = await Page.Locator("button[title='Edit Product']").AllAsync();
        
        if (editButtons.Count == 0)
        {
            // Create a product first
            await Page.GetByRole(AriaRole.Button, new() { Name = "Add Product" }).ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            var initialProductName = $"Product for Edit {Guid.NewGuid().ToString()[..8]}";
            await Page.GetByLabel("Product Name").FillAsync(initialProductName);
            await Page.GetByLabel("Quality Rating").SelectOptionAsync("Good");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Create Product" }).ClickAsync();
            await Page.WaitForURLAsync("**/products", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await Task.Delay(1000);
        }
        
        // Act - Click the first edit button
        editButtons = await Page.Locator("button[title='Edit Product']").AllAsync();
        Assert.NotEmpty(editButtons);
        await editButtons[0].ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Verify we're on the edit product page
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Edit Product" })).ToBeVisibleAsync();
        
        // Verify no errors on edit page
        var errorAlerts = await Page.Locator("[data-testid='error-alert']").AllAsync();
        Assert.Empty(errorAlerts);

        // Modify the product name
        var nameField = Page.GetByLabel("Product Name");
        var currentName = await nameField.InputValueAsync();
        var updatedProductName = $"{currentName} - Updated";
        
        await nameField.FillAsync(updatedProductName);
        
        // Optionally change other fields
        await Page.GetByLabel("Stock on Hand").FillAsync("25");

        // Submit the form
        await Page.GetByRole(AriaRole.Button, new() { Name = "Update Product" }).ClickAsync();
        
        // Wait for navigation back to products page
        await Page.WaitForURLAsync("**/products", new() { WaitUntil = WaitUntilState.NetworkIdle });
        
        // Assert - Verify we're back on products page
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Products" })).ToBeVisibleAsync();
        
        // Verify no error alerts
        errorAlerts = await Page.Locator("[data-testid='error-alert']").AllAsync();
        Assert.Empty(errorAlerts);

        // Verify the updated product name is visible in the list
        await Expect(Page.Locator($"text={updatedProductName}")).ToBeVisibleAsync(new() { Timeout = 5000 });
    }

    [Fact]
    public async Task ProductsPage_SearchFunctionality_Works()
    {
        // Arrange - Navigate to products page
        await NavigateToAppAsync("/products");
        await Page!.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(1000);
        
        // Verify no errors
        var errorAlerts = await Page.Locator("[data-testid='error-alert']").AllAsync();
        Assert.Empty(errorAlerts);

        // Check if there are products
        var productRows = await Page.Locator("table tbody tr").AllAsync();
        
        if (productRows.Count == 0)
        {
            // Skip test if no products exist
            outputHelper.WriteLine("No products found - skipping search test");
            return;
        }

        // Get the name of the first product
        var firstProductName = await productRows[0].Locator("strong").First.TextContentAsync();
        Assert.NotNull(firstProductName);
        
        // Act - Search for the product
        var searchBox = Page.GetByPlaceholder("Search products...");
        await searchBox.FillAsync(firstProductName);
        await searchBox.PressAsync("Enter");
        
        // Wait for search to complete
        await Task.Delay(1000);
        
        // Assert - Verify the product is still visible
        await Expect(Page.Locator($"text={firstProductName}")).ToBeVisibleAsync();
        
        // Verify no error alerts after search
        errorAlerts = await Page.Locator("[data-testid='error-alert']").AllAsync();
        Assert.Empty(errorAlerts);
    }
}
