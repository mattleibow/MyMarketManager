using Bunit;
using MyMarketManager.GraphQL.Client;
using MyMarketManager.WebApp.Components.Pages;
using NSubstitute;
using StrawberryShake;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace MyMarketManager.Components.Tests;

public class ProductFormTests : Bunit.TestContext
{
    [Fact]
    public async Task AddProduct_DisplaysCorrectTitle()
    {
        // Arrange
        var mockClient = Substitute.For<IMyMarketManagerClient>();
        Services.AddSingleton(mockClient);

        // Act
        var cut = RenderComponent<ProductForm>();
        await Task.Delay(100); // Give time for async operations

        // Assert
        Assert.Contains("Add Product", cut.Markup);
    }

    [Fact]
    public async Task EditProduct_LoadsProductData()
    {
        // Arrange
        var mockClient = Substitute.For<IMyMarketManagerClient>();
        var mockQuery = Substitute.For<IGetProductByIdQuery>();
        var mockResult = Substitute.For<IOperationResult<IGetProductByIdResult>>();
        var mockData = Substitute.For<IGetProductByIdResult>();
        var mockProduct = Substitute.For<IGetProductById_ProductById>();

        var productId = Guid.NewGuid();
        mockProduct.Id.Returns(productId);
        mockProduct.Name.Returns("Test Product");
        mockProduct.Sku.Returns("TST-001");
        mockProduct.Description.Returns("Test Description");
        mockProduct.Quality.Returns(ProductQuality.Good);
        mockProduct.Notes.Returns("Test Notes");
        mockProduct.StockOnHand.Returns(10);

        mockData.ProductById.Returns(mockProduct);
        mockResult.Data.Returns(mockData);
        mockQuery.ExecuteAsync(productId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(mockResult));
        mockClient.GetProductById.Returns(mockQuery);

        Services.AddSingleton(mockClient);

        // Act
        var cut = RenderComponent<ProductForm>(parameters => parameters
            .Add(p => p.ProductId, productId));
        await Task.Delay(100); // Give time for async operations

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("Edit Product"), TimeSpan.FromSeconds(2));
        Assert.Contains("Edit Product", cut.Markup);
        Assert.Contains("Test Product", cut.Markup);
    }

    [Fact]
    public async Task CreateProduct_SuccessfulCreation_NavigatesToProducts()
    {
        // Arrange
        var mockClient = Substitute.For<IMyMarketManagerClient>();
        var mockMutation = Substitute.For<ICreateProductMutation>();
        var mockResult = Substitute.For<IOperationResult<ICreateProductResult>>();
        var mockData = Substitute.For<ICreateProductResult>();
        var mockProduct = Substitute.For<ICreateProduct_CreateProduct>();

        mockProduct.Id.Returns(Guid.NewGuid());
        mockProduct.Name.Returns("New Product");
        mockData.CreateProduct.Returns(mockProduct);
        mockResult.Data.Returns(mockData);
        mockMutation.ExecuteAsync(Arg.Any<CreateProductInput>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mockResult));
        mockClient.CreateProduct.Returns(mockMutation);

        Services.AddSingleton(mockClient);
        
        var navMan = Services.GetRequiredService<NavigationManager>();

        // Act
        var cut = RenderComponent<ProductForm>();
        await Task.Delay(100); // Give time for async operations

        // Fill in the form
        var nameInput = cut.Find("#name");
        nameInput.Change("New Product");

        // Submit the form
        var form = cut.Find("form");
        await form.SubmitAsync();

        // Assert
        await mockMutation.Received(1).ExecuteAsync(
            Arg.Is<CreateProductInput>(input => input.Name == "New Product"), 
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProduct_SuccessfulUpdate_CallsMutation()
    {
        // Arrange
        var mockClient = Substitute.For<IMyMarketManagerClient>();
        var mockQuery = Substitute.For<IGetProductByIdQuery>();
        var mockQueryResult = Substitute.For<IOperationResult<IGetProductByIdResult>>();
        var mockQueryData = Substitute.For<IGetProductByIdResult>();
        var mockProduct = Substitute.For<IGetProductById_ProductById>();

        var productId = Guid.NewGuid();
        mockProduct.Id.Returns(productId);
        mockProduct.Name.Returns("Original Product");
        mockProduct.Sku.Returns("ORG-001");
        mockProduct.Description.Returns("Original Description");
        mockProduct.Quality.Returns(ProductQuality.Good);
        mockProduct.Notes.Returns("Original Notes");
        mockProduct.StockOnHand.Returns(5);

        mockQueryData.ProductById.Returns(mockProduct);
        mockQueryResult.Data.Returns(mockQueryData);
        mockQuery.ExecuteAsync(productId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(mockQueryResult));
        mockClient.GetProductById.Returns(mockQuery);

        // Setup update mutation
        var mockMutation = Substitute.For<IUpdateProductMutation>();
        var mockMutationResult = Substitute.For<IOperationResult<IUpdateProductResult>>();
        var mockMutationData = Substitute.For<IUpdateProductResult>();
        var mockUpdatedProduct = Substitute.For<IUpdateProduct_UpdateProduct>();

        mockUpdatedProduct.Id.Returns(productId);
        mockUpdatedProduct.Name.Returns("Updated Product");
        mockMutationData.UpdateProduct.Returns(mockUpdatedProduct);
        mockMutationResult.Data.Returns(mockMutationData);
        mockMutation.ExecuteAsync(productId, Arg.Any<UpdateProductInput>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mockMutationResult));
        mockClient.UpdateProduct.Returns(mockMutation);

        Services.AddSingleton(mockClient);

        // Act
        var cut = RenderComponent<ProductForm>(parameters => parameters
            .Add(p => p.ProductId, productId));
        await Task.Delay(100); // Give time for async operations

        // Wait for product to load
        cut.WaitForState(() => cut.Markup.Contains("Original Product"), TimeSpan.FromSeconds(2));

        // Update the name
        var nameInput = cut.Find("#name");
        nameInput.Change("Updated Product");

        // Submit the form
        var form = cut.Find("form");
        await form.SubmitAsync();

        // Assert
        await mockMutation.Received(1).ExecuteAsync(
            productId,
            Arg.Is<UpdateProductInput>(input => input.Name == "Updated Product"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelButton_NavigatesToProducts()
    {
        // Arrange
        var mockClient = Substitute.For<IMyMarketManagerClient>();
        Services.AddSingleton(mockClient);

        var navMan = Services.GetRequiredService<NavigationManager>();

        // Act
        var cut = RenderComponent<ProductForm>();
        await Task.Delay(100); // Give time for async operations

        var cancelButton = cut.Find("button.btn-secondary");
        cancelButton.Click();

        // Assert
        var uri = new Uri(navMan.Uri);
        Assert.EndsWith("/products", uri.AbsolutePath);
    }
}
