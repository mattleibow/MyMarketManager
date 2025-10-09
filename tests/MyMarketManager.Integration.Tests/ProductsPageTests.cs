using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace MyMarketManager.Integration.Tests;

/// <summary>
/// Playwright-based UI tests for the Products page, testing the full CRUD operations and search functionality
/// </summary>
public class ProductsPageTests(ITestOutputHelper outputHelper) : PlaywrightTestsBase(outputHelper)
{
    [Fact]
    [Trait(TestCategories.Key, TestCategories.Values.LongRunning)]
    public async Task ProductsList_DisplaysCorrectly()
    {
        // Arrange & Act
        await NavigateToAppAsync("/products");

        // Assert
        await Expect(Page!.GetByRole(AriaRole.Heading, new() { Name = "Products" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Add Product" })).ToBeVisibleAsync();
        await Expect(Page.GetByPlaceholder("Search products...")).ToBeVisibleAsync();
    }

    [Fact]
    [Trait(TestCategories.Key, TestCategories.Values.LongRunning)]
    public async Task CreateProduct_SuccessfullyCreatesAndDisplaysProduct()
    {
        // Arrange
        await NavigateToAppAsync("/products");
        var productName = $"Test Product {Guid.NewGuid():N}";

        // Act - Navigate to Add Product page
        await Page!.GetByRole(AriaRole.Link, new() { Name = "Add Product" }).ClickAsync();

        // Wait for form to load
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Add Product" })).ToBeVisibleAsync();

        // Fill in the form
        await Page.GetByLabel("Product Name").FillAsync(productName);
        await Page.GetByLabel("SKU").FillAsync("TEST-001");
        await Page.GetByLabel("Description").FillAsync("Test Description");
        await Page.GetByLabel("Quality Rating").SelectOptionAsync("GOOD");
        await Page.GetByLabel("Stock on Hand").FillAsync("10");
        await Page.GetByLabel("Notes").FillAsync("Test Notes");

        // Submit the form
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        // Assert - Should navigate back to products list
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Products" })).ToBeVisibleAsync();

        // Verify the product appears in the list
        await Expect(Page.GetByText(productName)).ToBeVisibleAsync();
    }

    [Fact]
    [Trait(TestCategories.Key, TestCategories.Values.LongRunning)]
    public async Task SearchProducts_FindsProductsByName()
    {
        // Arrange - Create a product first
        await NavigateToAppAsync("/products");
        var uniqueProductName = $"SearchTest {Guid.NewGuid():N}";
        
        await Page!.GetByRole(AriaRole.Link, new() { Name = "Add Product" }).ClickAsync();
        await Page.GetByLabel("Product Name").FillAsync(uniqueProductName);
        await Page.GetByLabel("Quality Rating").SelectOptionAsync("GOOD");
        await Page.GetByLabel("Stock on Hand").FillAsync("5");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Products" })).ToBeVisibleAsync();

        // Act - Search for the product
        var searchBox = Page.GetByPlaceholder("Search products...");
        await searchBox.FillAsync(uniqueProductName.Substring(0, 10)); // Search with partial name
        await searchBox.PressAsync("Enter");

        // Wait for search to complete
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Product should be visible in search results
        await Expect(Page.GetByText(uniqueProductName)).ToBeVisibleAsync();
    }

    [Fact]
    [Trait(TestCategories.Key, TestCategories.Values.LongRunning)]
    public async Task SearchProducts_IsCaseInsensitive()
    {
        // Arrange - Create a product
        await NavigateToAppAsync("/products");
        var uniqueProductName = $"CaseTest {Guid.NewGuid():N}";
        
        await Page!.GetByRole(AriaRole.Link, new() { Name = "Add Product" }).ClickAsync();
        await Page.GetByLabel("Product Name").FillAsync(uniqueProductName);
        await Page.GetByLabel("Quality Rating").SelectOptionAsync("EXCELLENT");
        await Page.GetByLabel("Stock on Hand").FillAsync("15");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Products" })).ToBeVisibleAsync();

        // Act - Search with different case
        var searchBox = Page.GetByPlaceholder("Search products...");
        await searchBox.FillAsync("casetest"); // lowercase search
        await searchBox.PressAsync("Enter");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Should find the product despite case difference
        await Expect(Page.GetByText(uniqueProductName)).ToBeVisibleAsync();
    }

    [Fact]
    [Trait(TestCategories.Key, TestCategories.Values.LongRunning)]
    public async Task UpdateProduct_SuccessfullyUpdatesProduct()
    {
        // Arrange - Create a product first
        await NavigateToAppAsync("/products");
        var originalName = $"UpdateTest {Guid.NewGuid():N}";
        var updatedName = $"Updated {Guid.NewGuid():N}";

        await Page!.GetByRole(AriaRole.Link, new() { Name = "Add Product" }).ClickAsync();
        await Page.GetByLabel("Product Name").FillAsync(originalName);
        await Page.GetByLabel("SKU").FillAsync("UPD-001");
        await Page.GetByLabel("Quality Rating").SelectOptionAsync("FAIR");
        await Page.GetByLabel("Stock on Hand").FillAsync("20");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Products" })).ToBeVisibleAsync();
        await Expect(Page.GetByText(originalName)).ToBeVisibleAsync();

        // Act - Edit the product
        // Find the row with the product and click edit button
        var productRow = Page.Locator("tr", new() { HasText = originalName });
        await productRow.GetByRole(AriaRole.Button, new() { Name = "Edit Product" }).ClickAsync();

        // Wait for edit form
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Edit Product" })).ToBeVisibleAsync();

        // Update the name
        var nameField = Page.GetByLabel("Product Name");
        await nameField.ClearAsync();
        await nameField.FillAsync(updatedName);

        // Save changes
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        // Assert - Should navigate back to products list with updated name
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Products" })).ToBeVisibleAsync();
        await Expect(Page.GetByText(updatedName)).ToBeVisibleAsync();
        await Expect(Page.GetByText(originalName)).Not.ToBeVisibleAsync();
    }

    [Fact]
    [Trait(TestCategories.Key, TestCategories.Values.LongRunning)]
    public async Task DeleteProduct_SuccessfullyRemovesProduct()
    {
        // Arrange - Create a product first
        await NavigateToAppAsync("/products");
        var productName = $"DeleteTest {Guid.NewGuid():N}";

        await Page!.GetByRole(AriaRole.Link, new() { Name = "Add Product" }).ClickAsync();
        await Page.GetByLabel("Product Name").FillAsync(productName);
        await Page.GetByLabel("Quality Rating").SelectOptionAsync("POOR");
        await Page.GetByLabel("Stock on Hand").FillAsync("3");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Products" })).ToBeVisibleAsync();
        await Expect(Page.GetByText(productName)).ToBeVisibleAsync();

        // Act - Delete the product
        var productRow = Page.Locator("tr", new() { HasText = productName });
        await productRow.GetByRole(AriaRole.Button, new() { Name = "Delete Product" }).ClickAsync();

        // Confirm deletion in modal
        await Expect(Page.GetByText("Are you sure you want to delete this product?")).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();

        // Wait for deletion to complete
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Product should no longer be visible
        await Expect(Page.GetByText(productName)).Not.ToBeVisibleAsync();
    }

    [Fact]
    [Trait(TestCategories.Key, TestCategories.Values.LongRunning)]
    public async Task CancelProductCreation_NavigatesBackWithoutSaving()
    {
        // Arrange
        await NavigateToAppAsync("/products");

        // Act - Navigate to Add Product and cancel
        await Page!.GetByRole(AriaRole.Link, new() { Name = "Add Product" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Add Product" })).ToBeVisibleAsync();

        // Fill some data
        await Page.GetByLabel("Product Name").FillAsync("Should Not Save");

        // Click Cancel
        await Page.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();

        // Assert - Should be back on products list
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Products" })).ToBeVisibleAsync();

        // Product should not exist
        await Expect(Page.GetByText("Should Not Save")).Not.ToBeVisibleAsync();
    }

    [Fact]
    [Trait(TestCategories.Key, TestCategories.Values.LongRunning)]
    public async Task ProductForm_ValidatesRequiredFields()
    {
        // Arrange
        await NavigateToAppAsync("/products/add");

        // Act - Try to submit without filling required fields
        await Page!.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        // Assert - Should show validation errors and stay on form
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Add Product" })).ToBeVisibleAsync();
        
        // Form should display validation messages (Blazor validation)
        var nameField = Page.GetByLabel("Product Name");
        await Expect(nameField).ToBeVisibleAsync();
    }
}
