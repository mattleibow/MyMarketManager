using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake;
using MyMarketManager.Components.Tests.Components;
using MyMarketManager.GraphQL.Client;
using FluentAssertions;

namespace MyMarketManager.Components.Tests;

public class ProductsGraphQLTests : Bunit.TestContext
{
    [Fact]
    public void Products_RendersLoadingState_Initially()
    {
        // Arrange
        var mockClient = Substitute.For<IMyMarketManagerClient>();
        var mockQuery = Substitute.For<IGetProductsQuery>();
        
        // Create a task that never completes to simulate loading
        var tcs = new TaskCompletionSource<IOperationResult<IGetProductsResult>>();
        mockQuery.ExecuteAsync(Arg.Any<CancellationToken>()).Returns(tcs.Task);
        mockClient.GetProducts.Returns(mockQuery);
        
        Services.AddSingleton(mockClient);
        JSInterop.Mode = JSRuntimeMode.Loose;
        
        // Act
        var cut = RenderComponent<ProductsGraphQL>();
        
        // Assert
        cut.Find(".spinner-border").Should().NotBeNull();
        cut.Markup.Should().Contain("Loading...");
    }
    
    [Fact]
    public async Task Products_DisplaysProducts_WhenDataLoaded()
    {
        // Arrange
        var mockClient = Substitute.For<IMyMarketManagerClient>();
        var mockQuery = Substitute.For<IGetProductsQuery>();
        var mockResult = Substitute.For<IOperationResult<IGetProductsResult>>();
        var mockData = Substitute.For<IGetProductsResult>();
        var mockProduct = Substitute.For<IGetProducts_Products>();
        
        mockProduct.Id.Returns(Guid.NewGuid());
        mockProduct.Name.Returns("Test Product");
        mockProduct.Sku.Returns("TEST-001");
        mockProduct.Description.Returns("Test Description");
        mockProduct.Quality.Returns(ProductQuality.Good);
        mockProduct.StockOnHand.Returns(10);
        
        mockData.Products.Returns(new List<IGetProducts_Products> { mockProduct });
        mockResult.Data.Returns(mockData);
        mockResult.IsErrorResult().Returns(false);
        
        mockQuery.ExecuteAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(mockResult));
        mockClient.GetProducts.Returns(mockQuery);
        
        Services.AddSingleton(mockClient);
        JSInterop.Mode = JSRuntimeMode.Loose;
        
        // Act
        var cut = RenderComponent<ProductsGraphQL>();
        await Task.Delay(100); // Allow component to complete initialization
        
        // Assert
        cut.Markup.Should().Contain("Test Product");
        cut.Markup.Should().Contain("TEST-001");
        cut.Markup.Should().Contain("Test Description");
    }
    
    [Fact]
    public async Task Products_DisplaysEmptyMessage_WhenNoProducts()
    {
        // Arrange
        var mockClient = Substitute.For<IMyMarketManagerClient>();
        var mockQuery = Substitute.For<IGetProductsQuery>();
        var mockResult = Substitute.For<IOperationResult<IGetProductsResult>>();
        var mockData = Substitute.For<IGetProductsResult>();
        
        mockData.Products.Returns(new List<IGetProducts_Products>());
        mockResult.Data.Returns(mockData);
        mockResult.IsErrorResult().Returns(false);
        
        mockQuery.ExecuteAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(mockResult));
        mockClient.GetProducts.Returns(mockQuery);
        
        Services.AddSingleton(mockClient);
        JSInterop.Mode = JSRuntimeMode.Loose;
        
        // Act
        var cut = RenderComponent<ProductsGraphQL>();
        await Task.Delay(100); // Allow component to complete initialization
        
        // Assert
        cut.Markup.Should().Contain("No products found");
    }
    
    [Fact]
    public async Task Products_DisplaysErrorMessage_WhenLoadFails()
    {
        // Arrange
        var mockClient = Substitute.For<IMyMarketManagerClient>();
        var mockQuery = Substitute.For<IGetProductsQuery>();
        var mockResult = Substitute.For<IOperationResult<IGetProductsResult>>();
        var mockError = Substitute.For<IClientError>();
        
        mockError.Message.Returns("Test error message");
        mockResult.IsErrorResult().Returns(true);
        mockResult.Errors.Returns(new List<IClientError> { mockError });
        
        mockQuery.ExecuteAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(mockResult));
        mockClient.GetProducts.Returns(mockQuery);
        
        Services.AddSingleton(mockClient);
        JSInterop.Mode = JSRuntimeMode.Loose;
        
        // Act
        var cut = RenderComponent<ProductsGraphQL>();
        await Task.Delay(100); // Allow component to complete initialization
        
        // Assert
        cut.Markup.Should().Contain("Test error message");
    }
    
    [Fact]
    public async Task Products_OpensDeleteModal_WhenDeleteButtonClicked()
    {
        // Arrange
        var mockClient = Substitute.For<IMyMarketManagerClient>();
        var mockQuery = Substitute.For<IGetProductsQuery>();
        var mockResult = Substitute.For<IOperationResult<IGetProductsResult>>();
        var mockData = Substitute.For<IGetProductsResult>();
        var mockProduct = Substitute.For<IGetProducts_Products>();
        
        var productId = Guid.NewGuid();
        mockProduct.Id.Returns(productId);
        mockProduct.Name.Returns("Test Product");
        mockProduct.Sku.Returns("TEST-001");
        mockProduct.Quality.Returns(ProductQuality.Good);
        mockProduct.StockOnHand.Returns(5);
        
        mockData.Products.Returns(new List<IGetProducts_Products> { mockProduct });
        mockResult.Data.Returns(mockData);
        mockResult.IsErrorResult().Returns(false);
        
        mockQuery.ExecuteAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(mockResult));
        mockClient.GetProducts.Returns(mockQuery);
        
        Services.AddSingleton(mockClient);
        JSInterop.Mode = JSRuntimeMode.Loose;
        
        // Act
        var cut = RenderComponent<ProductsGraphQL>();
        await Task.Delay(100); // Allow component to complete initialization
        
        var deleteButton = cut.FindAll("button").FirstOrDefault(b => b.GetAttribute("title") == "Delete Product");
        deleteButton.Should().NotBeNull();
        deleteButton!.Click();
        
        // Assert
        cut.Markup.Should().Contain("Delete Product");
        cut.Markup.Should().Contain("Are you sure you want to delete");
        cut.Markup.Should().Contain("Test Product");
    }
    
    [Fact]
    public async Task Products_DeletesProduct_WhenConfirmed()
    {
        // Arrange
        var mockClient = Substitute.For<IMyMarketManagerClient>();
        var mockQuery = Substitute.For<IGetProductsQuery>();
        var mockDeleteMutation = Substitute.For<IDeleteProductMutation>();
        var mockResult = Substitute.For<IOperationResult<IGetProductsResult>>();
        var mockDeleteResult = Substitute.For<IOperationResult<IDeleteProductResult>>();
        var mockData = Substitute.For<IGetProductsResult>();
        var mockProduct = Substitute.For<IGetProducts_Products>();
        
        var productId = Guid.NewGuid();
        mockProduct.Id.Returns(productId);
        mockProduct.Name.Returns("Test Product");
        mockProduct.Sku.Returns("TEST-001");
        mockProduct.Quality.Returns(ProductQuality.Good);
        mockProduct.StockOnHand.Returns(0);
        
        mockData.Products.Returns(new List<IGetProducts_Products> { mockProduct });
        mockResult.Data.Returns(mockData);
        mockResult.IsErrorResult().Returns(false);
        
        mockDeleteResult.IsErrorResult().Returns(false);
        
        mockQuery.ExecuteAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(mockResult));
        mockDeleteMutation.ExecuteAsync(productId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(mockDeleteResult));
        
        mockClient.GetProducts.Returns(mockQuery);
        mockClient.DeleteProduct.Returns(mockDeleteMutation);
        
        Services.AddSingleton(mockClient);
        JSInterop.Mode = JSRuntimeMode.Loose;
        
        // Act
        var cut = RenderComponent<ProductsGraphQL>();
        await Task.Delay(100); // Allow component to complete initialization
        
        // Click delete button to open modal
        var deleteButton = cut.FindAll("button").FirstOrDefault(b => b.GetAttribute("title") == "Delete Product");
        deleteButton!.Click();
        
        // Click confirm delete in modal
        var confirmButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Delete") && b.ClassName?.Contains("btn-danger") == true);
        confirmButton!.Click();
        
        await Task.Delay(100); // Allow deletion to complete
        
        // Assert
        await mockDeleteMutation.Received(1).ExecuteAsync(productId, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task Products_FiltersProducts_WhenSearching()
    {
        // Arrange
        var mockClient = Substitute.For<IMyMarketManagerClient>();
        var mockQuery = Substitute.For<IGetProductsQuery>();
        var mockResult = Substitute.For<IOperationResult<IGetProductsResult>>();
        var mockData = Substitute.For<IGetProductsResult>();
        
        var product1 = Substitute.For<IGetProducts_Products>();
        product1.Id.Returns(Guid.NewGuid());
        product1.Name.Returns("Apple");
        product1.Quality.Returns(ProductQuality.Good);
        product1.StockOnHand.Returns(10);
        
        var product2 = Substitute.For<IGetProducts_Products>();
        product2.Id.Returns(Guid.NewGuid());
        product2.Name.Returns("Banana");
        product2.Quality.Returns(ProductQuality.Good);
        product2.StockOnHand.Returns(5);
        
        mockData.Products.Returns(new List<IGetProducts_Products> { product1, product2 });
        mockResult.Data.Returns(mockData);
        mockResult.IsErrorResult().Returns(false);
        
        mockQuery.ExecuteAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(mockResult));
        mockClient.GetProducts.Returns(mockQuery);
        
        Services.AddSingleton(mockClient);
        JSInterop.Mode = JSRuntimeMode.Loose;
        
        // Act
        var cut = RenderComponent<ProductsGraphQL>();
        await Task.Delay(100); // Allow component to complete initialization
        
        // Initially both products should be visible
        cut.Markup.Should().Contain("Apple");
        cut.Markup.Should().Contain("Banana");
        
        // Type in search box
        var searchInput = cut.Find("input[placeholder='Search products...']");
        searchInput.Change("apple");
        
        // Click search button
        var searchButton = cut.FindAll("button").FirstOrDefault(b => b.GetAttribute("title") == "Search");
        searchButton!.Click();
        
        // Assert
        cut.Markup.Should().Contain("Apple");
        cut.Markup.Should().NotContain("Banana");
    }
}
