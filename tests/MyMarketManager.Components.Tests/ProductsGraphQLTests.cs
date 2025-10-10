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
    public void Products_RendersTitle()
    {
        // Arrange  
        var mockClient = Substitute.For<IMyMarketManagerClient>();
        var mockQuery = Substitute.For<IGetProductsQuery>();
        
        // Return empty result
        var emptyResult = OperationResultHelper.CreateSuccessResult<IGetProductsResult>();
        mockQuery.ExecuteAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(emptyResult));
        mockClient.GetProducts.Returns(mockQuery);
        
        Services.AddSingleton(mockClient);
        JSInterop.Mode = JSRuntimeMode.Loose;
        
        // Act
        var cut = RenderComponent<ProductsGraphQL>();
        
        // Assert
        cut.Find("h1").TextContent.Should().Contain("Products");
    }
}

// Helper to create operation results without complex mocking
internal static class OperationResultHelper
{
    public static IOperationResult<T> CreateSuccessResult<T>() where T : class
    {
        var result = Substitute.For<IOperationResult<T>>();
        result.Errors.Returns((IReadOnlyList<IClientError>?)null);
        result.Data.Returns((T?)null);
        return result;
    }
}
