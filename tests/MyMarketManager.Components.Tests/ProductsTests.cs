using Bunit;
using MyMarketManager.GraphQL.Client;
using MyMarketManager.WebApp.Components.Pages;
using NSubstitute;
using StrawberryShake;
using Microsoft.Extensions.DependencyInjection;

namespace MyMarketManager.Components.Tests;

public class ProductsTests : Bunit.TestContext
{
    [Fact]
    public async Task LoadProducts_SuccessfulQuery_DisplaysProducts()
    {
        // Arrange
        var mockClient = Substitute.For<IMyMarketManagerClient>();
        var mockQuery = Substitute.For<IGetProductsQuery>();
        var mockResult = Substitute.For<IOperationResult<IGetProductsResult>>();
        var mockData = Substitute.For<IGetProductsResult>();

        var products = new List<IGetProducts_Products>
        {
            CreateMockProduct(Guid.NewGuid(), "Product 1", "PRD-001", ProductQuality.Good, 10),
            CreateMockProduct(Guid.NewGuid(), "Product 2", "PRD-002", ProductQuality.Excellent, 5)
        };

        mockData.Products.Returns(products);
        mockResult.Data.Returns(mockData);
        mockQuery.ExecuteAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(mockResult));
        mockClient.GetProducts.Returns(mockQuery);

        Services.AddSingleton(mockClient);

        // Act
        var cut = RenderComponent<Products>();
        await Task.Delay(100); // Give time for async operations

        // Assert
        cut.WaitForState(() => cut.FindAll("tbody tr").Count == 2, TimeSpan.FromSeconds(2));
        var rows = cut.FindAll("tbody tr");
        Assert.Equal(2, rows.Count);
        Assert.Contains("Product 1", cut.Markup);
        Assert.Contains("Product 2", cut.Markup);
    }

    [Fact]
    public async Task LoadProducts_EmptyResult_DisplaysNoProductsMessage()
    {
        // Arrange
        var mockClient = Substitute.For<IMyMarketManagerClient>();
        var mockQuery = Substitute.For<IGetProductsQuery>();
        var mockResult = Substitute.For<IOperationResult<IGetProductsResult>>();
        var mockData = Substitute.For<IGetProductsResult>();

        mockData.Products.Returns(new List<IGetProducts_Products>());
        mockResult.Data.Returns(mockData);
        mockQuery.ExecuteAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(mockResult));
        mockClient.GetProducts.Returns(mockQuery);

        Services.AddSingleton(mockClient);

        // Act
        var cut = RenderComponent<Products>();
        await Task.Delay(100); // Give time for async operations

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("No products found"), TimeSpan.FromSeconds(2));
        Assert.Contains("No products found", cut.Markup);
    }

    [Fact]
    public async Task DeleteProduct_SuccessfulDeletion_RemovesProduct()
    {
        // Arrange
        var mockClient = Substitute.For<IMyMarketManagerClient>();
        var mockQuery = Substitute.For<IGetProductsQuery>();
        var mockDeleteMutation = Substitute.For<IDeleteProductMutation>();
        
        var productId = Guid.NewGuid();
        var products = new List<IGetProducts_Products>
        {
            CreateMockProduct(productId, "Product 1", "PRD-001", ProductQuality.Good, 10)
        };

        // Setup initial query
        var mockResult = Substitute.For<IOperationResult<IGetProductsResult>>();
        var mockData = Substitute.For<IGetProductsResult>();
        mockData.Products.Returns(products);
        mockResult.Data.Returns(mockData);
        mockQuery.ExecuteAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(mockResult));
        mockClient.GetProducts.Returns(mockQuery);

        // Setup delete mutation
        var mockDeleteResult = Substitute.For<IOperationResult<IDeleteProductResult>>();
        var mockDeleteData = Substitute.For<IDeleteProductResult>();
        mockDeleteData.DeleteProduct.Returns(true);
        mockDeleteResult.Data.Returns(mockDeleteData);
        mockDeleteMutation.ExecuteAsync(productId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(mockDeleteResult));
        mockClient.DeleteProduct.Returns(mockDeleteMutation);

        Services.AddSingleton(mockClient);

        // Act
        var cut = RenderComponent<Products>();
        await Task.Delay(100); // Give time for async operations

        // Find and click delete button
        cut.WaitForState(() => cut.FindAll("button[title='Delete Product']").Count > 0, TimeSpan.FromSeconds(2));
        var deleteButton = cut.Find("button[title='Delete Product']");
        deleteButton.Click();

        // Confirm deletion in modal
        await Task.Delay(50);
        var confirmButton = cut.Find("button.btn-danger");
        confirmButton.Click();

        // Assert
        await mockDeleteMutation.Received(1).ExecuteAsync(productId, Arg.Any<CancellationToken>());
    }

    private static IGetProducts_Products CreateMockProduct(
        Guid id, 
        string name, 
        string sku, 
        ProductQuality quality, 
        int stock)
    {
        var product = Substitute.For<IGetProducts_Products>();
        product.Id.Returns(id);
        product.Name.Returns(name);
        product.Sku.Returns(sku);
        product.Quality.Returns(quality);
        product.StockOnHand.Returns(stock);
        product.Description.Returns($"Description for {name}");
        return product;
    }
}
