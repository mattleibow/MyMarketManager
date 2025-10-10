using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake;
using MyMarketManager.Components.Tests.Components;
using MyMarketManager.GraphQL.Client;
using FluentAssertions;

namespace MyMarketManager.Components.Tests;

public class ProductFormGraphQLTests : Bunit.TestContext
{
    [Fact]
    public void ProductForm_RendersAddForm_WhenNoProductId()
    {
        // Arrange
        var mockClient = Substitute.For<IMyMarketManagerClient>();
        
        Services.AddSingleton(mockClient);
        JSInterop.Mode = JSRuntimeMode.Loose;
        
        // Act
        var cut = RenderComponent<ProductFormGraphQL>();
        
        // Assert
        cut.Markup.Should().Contain("Add Product");
        cut.Markup.Should().Contain("Create Product");
    }
    
    [Fact]
    public async Task ProductForm_LoadsProduct_WhenProductIdProvided()
    {
        // Arrange
        var mockClient = Substitute.For<IMyMarketManagerClient>();
        var mockQuery = Substitute.For<IGetProductByIdQuery>();
        var mockResult = Substitute.For<IOperationResult<IGetProductByIdResult>>();
        var mockData = Substitute.For<IGetProductByIdResult>();
        var mockProduct = Substitute.For<IGetProductById_ProductById>();
        
        var productId = Guid.NewGuid();
        mockProduct.Id.Returns(productId);
        mockProduct.Name.Returns("Existing Product");
        mockProduct.Sku.Returns("EXIST-001");
        mockProduct.Description.Returns("Existing Description");
        mockProduct.Quality.Returns(ProductQuality.Excellent);
        mockProduct.Notes.Returns("Some notes");
        mockProduct.StockOnHand.Returns(25);
        
        mockData.ProductById.Returns(mockProduct);
        mockResult.Data.Returns(mockData);
        mockResult.IsErrorResult().Returns(false);
        
        mockQuery.ExecuteAsync(productId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(mockResult));
        mockClient.GetProductById.Returns(mockQuery);
        
        Services.AddSingleton(mockClient);
        JSInterop.Mode = JSRuntimeMode.Loose;
        
        // Act
        var parameters = new[] { ComponentParameter.CreateParameter("ProductId", (Guid?)productId) };
        var cut = RenderComponent<ProductFormGraphQL>(parameters);
        await Task.Delay(100); // Allow component to complete initialization
        
        // Assert
        cut.Markup.Should().Contain("Edit Product");
        cut.Markup.Should().Contain("Update Product");
        
        var nameInput = cut.Find("#name");
        nameInput.GetAttribute("value").Should().Be("Existing Product");
    }
    
    [Fact]
    public async Task ProductForm_CreatesProduct_WhenFormSubmitted()
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
        mockResult.IsErrorResult().Returns(false);
        
        mockMutation.ExecuteAsync(Arg.Any<CreateProductInput>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mockResult));
        mockClient.CreateProduct.Returns(mockMutation);
        
        Services.AddSingleton(mockClient);
        JSInterop.Mode = JSRuntimeMode.Loose;
        
        var mockNavManager = Substitute.For<NavigationManager>();
        Services.AddSingleton(mockNavManager);
        
        // Act
        var cut = RenderComponent<ProductFormGraphQL>();
        
        // Fill in the form
        var nameInput = cut.Find("#name");
        nameInput.Change("New Product");
        
        var stockInput = cut.Find("#stock");
        stockInput.Change(10);
        
        // Submit the form
        var form = cut.Find("form");
        await form.SubmitAsync();
        await Task.Delay(100); // Allow mutation to complete
        
        // Assert
        await mockMutation.Received(1).ExecuteAsync(
            Arg.Is<CreateProductInput>(i => i.Name == "New Product" && i.StockOnHand == 10),
            Arg.Any<CancellationToken>()
        );
    }
    
    [Fact]
    public async Task ProductForm_UpdatesProduct_WhenEditingExisting()
    {
        // Arrange
        var mockClient = Substitute.For<IMyMarketManagerClient>();
        var mockQuery = Substitute.For<IGetProductByIdQuery>();
        var mockMutation = Substitute.For<IUpdateProductMutation>();
        var mockQueryResult = Substitute.For<IOperationResult<IGetProductByIdResult>>();
        var mockMutationResult = Substitute.For<IOperationResult<IUpdateProductResult>>();
        var mockQueryData = Substitute.For<IGetProductByIdResult>();
        var mockMutationData = Substitute.For<IUpdateProductResult>();
        var mockProduct = Substitute.For<IGetProductById_ProductById>();
        var mockUpdatedProduct = Substitute.For<IUpdateProduct_UpdateProduct>();
        
        var productId = Guid.NewGuid();
        mockProduct.Id.Returns(productId);
        mockProduct.Name.Returns("Original Name");
        mockProduct.Sku.Returns("ORIG-001");
        mockProduct.Quality.Returns(ProductQuality.Good);
        mockProduct.StockOnHand.Returns(5);
        
        mockQueryData.ProductById.Returns(mockProduct);
        mockQueryResult.Data.Returns(mockQueryData);
        mockQueryResult.IsErrorResult().Returns(false);
        
        mockUpdatedProduct.Id.Returns(productId);
        mockUpdatedProduct.Name.Returns("Updated Name");
        
        mockMutationData.UpdateProduct.Returns(mockUpdatedProduct);
        mockMutationResult.Data.Returns(mockMutationData);
        mockMutationResult.IsErrorResult().Returns(false);
        
        mockQuery.ExecuteAsync(productId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(mockQueryResult));
        mockMutation.ExecuteAsync(productId, Arg.Any<UpdateProductInput>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mockMutationResult));
        
        mockClient.GetProductById.Returns(mockQuery);
        mockClient.UpdateProduct.Returns(mockMutation);
        
        Services.AddSingleton(mockClient);
        JSInterop.Mode = JSRuntimeMode.Loose;
        
        var mockNavManager = Substitute.For<NavigationManager>();
        Services.AddSingleton(mockNavManager);
        
        // Act
        var parameters = new[] { ComponentParameter.CreateParameter("ProductId", (Guid?)productId) };
        var cut = RenderComponent<ProductFormGraphQL>(parameters);
        await Task.Delay(100); // Allow component to complete initialization
        
        // Update the name
        var nameInput = cut.Find("#name");
        nameInput.Change("Updated Name");
        
        // Submit the form
        var form = cut.Find("form");
        await form.SubmitAsync();
        await Task.Delay(100); // Allow mutation to complete
        
        // Assert
        await mockMutation.Received(1).ExecuteAsync(
            productId,
            Arg.Is<UpdateProductInput>(i => i.Name == "Updated Name"),
            Arg.Any<CancellationToken>()
        );
    }
    
    [Fact]
    public async Task ProductForm_DisplaysError_WhenCreationFails()
    {
        // Arrange
        var mockClient = Substitute.For<IMyMarketManagerClient>();
        var mockMutation = Substitute.For<ICreateProductMutation>();
        var mockResult = Substitute.For<IOperationResult<ICreateProductResult>>();
        var mockError = Substitute.For<IClientError>();
        
        mockError.Message.Returns("Creation failed");
        mockResult.IsErrorResult().Returns(true);
        mockResult.Errors.Returns(new List<IClientError> { mockError });
        
        mockMutation.ExecuteAsync(Arg.Any<CreateProductInput>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mockResult));
        mockClient.CreateProduct.Returns(mockMutation);
        
        Services.AddSingleton(mockClient);
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.Setup<object>("alert", _ => true);
        
        // Act
        var cut = RenderComponent<ProductFormGraphQL>();
        
        var nameInput = cut.Find("#name");
        nameInput.Change("New Product");
        
        var form = cut.Find("form");
        await form.SubmitAsync();
        await Task.Delay(100); // Allow mutation to complete
        
        // Assert
        cut.Markup.Should().Contain("Creation failed");
    }
    
    [Fact]
    public void ProductForm_CancelsAndNavigates_WhenCancelClicked()
    {
        // Arrange
        var mockClient = Substitute.For<IMyMarketManagerClient>();
        Services.AddSingleton(mockClient);
        JSInterop.Mode = JSRuntimeMode.Loose;
        
        var mockNavManager = Substitute.For<NavigationManager>();
        Services.AddSingleton(mockNavManager);
        
        // Act
        var cut = RenderComponent<ProductFormGraphQL>();
        
        var cancelButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Cancel"));
        cancelButton.Should().NotBeNull();
        cancelButton!.Click();
        
        // Assert
        mockNavManager.Received(1).NavigateTo("/products-graphql");
    }
    
    [Fact]
    public async Task ProductForm_InitializesWithDefaults_ForNewProduct()
    {
        // Arrange
        var mockClient = Substitute.For<IMyMarketManagerClient>();
        Services.AddSingleton(mockClient);
        JSInterop.Mode = JSRuntimeMode.Loose;
        
        // Act
        var cut = RenderComponent<ProductFormGraphQL>();
        await Task.Delay(100); // Allow component to complete initialization
        
        // Assert
        var qualitySelect = cut.Find("#quality");
        var stockInput = cut.Find("#stock");
        
        qualitySelect.GetAttribute("value").Should().Be(ProductQuality.Good.ToString());
        stockInput.GetAttribute("value").Should().Be("0");
    }
}
