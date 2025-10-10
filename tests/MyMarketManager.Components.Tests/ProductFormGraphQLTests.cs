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
    public void ProductForm_RendersEditForm_WhenProductIdProvided()
    {
        // Arrange
        var mockClient = Substitute.For<IMyMarketManagerClient>();
        var mockQuery = Substitute.For<IGetProductByIdQuery>();
        
        // Create a task that never completes to keep loading state
        var tcs = new TaskCompletionSource<IOperationResult<IGetProductByIdResult>>();
        var productId = Guid.NewGuid();
        mockQuery.ExecuteAsync(productId, Arg.Any<CancellationToken>()).Returns(tcs.Task);
        mockClient.GetProductById.Returns(mockQuery);
        
        Services.AddSingleton(mockClient);
        JSInterop.Mode = JSRuntimeMode.Loose;
        
        // Act
        var parameters = new[] { ComponentParameter.CreateParameter("ProductId", (Guid?)productId) };
        var cut = RenderComponent<ProductFormGraphQL>(parameters);
        
        // Assert  
        cut.Markup.Should().Contain("Edit Product");
    }
    
    [Fact]
    public void ProductForm_HasFormFields()
    {
        // Arrange
        var mockClient = Substitute.For<IMyMarketManagerClient>();
        
        Services.AddSingleton(mockClient);
        JSInterop.Mode = JSRuntimeMode.Loose;
        
        // Act
        var cut = RenderComponent<ProductFormGraphQL>();
        
        // Assert - check all expected form fields exist
        cut.Find("#name").Should().NotBeNull();
        cut.Find("#sku").Should().NotBeNull();
        cut.Find("#description").Should().NotBeNull();
        cut.Find("#quality").Should().NotBeNull();
        cut.Find("#stock").Should().NotBeNull();
        cut.Find("#notes").Should().NotBeNull();
    }
}
