# GraphQL Client Documentation

This document describes the GraphQL client library for MyMarketManager, built with StrawberryShake.

## Overview

The MyMarketManager.GraphQL.Client library provides a strongly-typed GraphQL client for accessing the MyMarketManager GraphQL API. It uses [StrawberryShake 15](https://chillicream.com/docs/strawberryshake) to generate type-safe client code from the GraphQL schema.

**Key Features:**
- **Type-Safe**: All GraphQL operations are strongly typed with generated C# classes
- **Cross-Platform**: Works with .NET 10, MAUI, Blazor WASM, Blazor Server, ASP.NET Core
- **Auto-Generated**: Client code is automatically generated from the running GraphQL server schema
- **Dependency Injection**: First-class support for `Microsoft.Extensions.DependencyInjection`
- **Async/Await**: All operations use Task-based asynchronous patterns
- **Error Handling**: Structured error handling with detailed error information

## Installation

### As a Project Reference

Add a project reference in your `.csproj` file:

```xml
<ItemGroup>
  <ProjectReference Include="../MyMarketManager.GraphQL.Client/MyMarketManager.GraphQL.Client.csproj" />
</ItemGroup>
```

Or via command line:

```bash
dotnet add reference path/to/MyMarketManager.GraphQL.Client/MyMarketManager.GraphQL.Client.csproj
```

## Service Registration

All platforms use the `AddMyMarketManagerClient()` extension method to register the GraphQL client.

### MAUI Mobile App

In `MauiProgram.cs`:

```csharp
using MyMarketManager.GraphQL.Client;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>();

        // Register the GraphQL client with the remote API URL
        builder.Services.AddMyMarketManagerClient(
            "https://your-production-api.com/graphql");

        return builder.Build();
    }
}
```

### Blazor WASM

In `Program.cs`:

```csharp
using MyMarketManager.GraphQL.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Register the GraphQL client
builder.Services.AddMyMarketManagerClient(
    "https://your-production-api.com/graphql");

await builder.Build().RunAsync();
```

### Blazor Server / ASP.NET Core

For local development (same host):

```csharp
using MyMarketManager.GraphQL.Client;

var builder = WebApplication.CreateBuilder(args);

// Option 1: Relative URL (for same-host scenarios)
builder.Services.AddMyMarketManagerClient();

// Option 2: Explicit remote URL
// builder.Services.AddMyMarketManagerClient("https://your-api.com/graphql");

var app = builder.Build();
```

## Using the Client

Inject `IMyMarketManagerClient` into your classes:

```csharp
public class ProductsViewModel
{
    private readonly IMyMarketManagerClient _client;
    
    public ProductsViewModel(IMyMarketManagerClient client)
    {
        _client = client;
    }
    
    public async Task<List<ProductDto>> LoadProductsAsync()
    {
        var result = await _client.GetProducts.ExecuteAsync();
        
        if (result.IsSuccess && result.Data?.Products != null)
        {
            return result.Data.Products.ToList();
        }
        
        // Handle errors
        if (result.Errors != null)
        {
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"Error: {error.Message}");
            }
        }
        
        return new List<ProductDto>();
    }
}
```

## Available Operations

### Queries

#### GetProducts

Get all products with all available fields.

**Generated Method:**
```csharp
IOperationResult<IGetProductsResult> result = 
    await client.GetProducts.ExecuteAsync(cancellationToken);
```

**Example Usage:**
```csharp
public async Task<List<Product>> GetAllProducts()
{
    var result = await _client.GetProducts.ExecuteAsync();
    
    if (result.IsSuccess && result.Data?.Products != null)
    {
        return result.Data.Products
            .Select(p => new Product
            {
                Id = p.Id,
                Name = p.Name,
                SKU = p.Sku,
                Quality = p.Quality,
                StockOnHand = p.StockOnHand,
                Description = p.Description,
                Notes = p.Notes
            })
            .ToList();
    }
    
    return new List<Product>();
}
```

#### GetProductById

Get a specific product by ID.

**Generated Method:**
```csharp
IOperationResult<IGetProductByIdResult> result = 
    await client.GetProductById.ExecuteAsync(productId, cancellationToken);
```

**Example Usage:**
```csharp
public async Task<Product?> GetProductById(Guid productId)
{
    var result = await _client.GetProductById.ExecuteAsync(productId);
    
    if (result.IsSuccess && result.Data?.ProductById != null)
    {
        var p = result.Data.ProductById;
        return new Product
        {
            Id = p.Id,
            Name = p.Name,
            SKU = p.Sku,
            Quality = p.Quality,
            StockOnHand = p.StockOnHand,
            Description = p.Description,
            Notes = p.Notes
        };
    }
    
    return null;
}
```

### Mutations

#### CreateProduct

Create a new product.

**Example Usage:**
```csharp
public async Task<Product?> CreateProduct(string name, string? sku, ProductQuality quality, int stockOnHand)
{
    var input = new CreateProductInput
    {
        Name = name,
        Sku = sku,
        Quality = quality,
        StockOnHand = stockOnHand,
        Description = null,
        Notes = null
    };
    
    var result = await _client.CreateProduct.ExecuteAsync(input);
    
    if (result.IsSuccess && result.Data?.CreateProduct != null)
    {
        var p = result.Data.CreateProduct;
        return new Product
        {
            Id = p.Id,
            Name = p.Name,
            SKU = p.Sku,
            Quality = p.Quality,
            StockOnHand = p.StockOnHand
        };
    }
    
    return null;
}
```

#### UpdateProduct

Update an existing product.

**Example Usage:**
```csharp
public async Task<bool> UpdateProduct(Guid productId, string name, string? sku, ProductQuality quality, int stockOnHand)
{
    var input = new UpdateProductInput
    {
        Name = name,
        Sku = sku,
        Quality = quality,
        StockOnHand = stockOnHand,
        Description = null,
        Notes = null
    };
    
    var result = await _client.UpdateProduct.ExecuteAsync(productId, input);
    
    return result.IsSuccess && result.Data?.UpdateProduct != null;
}
```

#### DeleteProduct

Delete a product.

**Example Usage:**
```csharp
public async Task<bool> DeleteProduct(Guid productId)
{
    var result = await _client.DeleteProduct.ExecuteAsync(productId);
    
    if (result.IsSuccess && result.Data != null)
    {
        return result.Data.DeleteProduct;
    }
    
    return false;
}
```

## Error Handling

StrawberryShake provides structured error handling:

```csharp
var result = await _client.GetProducts.ExecuteAsync();

// Check if operation succeeded
if (result.IsSuccess)
{
    // Use result.Data
    var products = result.Data.Products;
}
else
{
    // Handle errors
    if (result.Errors != null)
    {
        foreach (var error in result.Errors)
        {
            Console.WriteLine($"Error: {error.Message}");
            Console.WriteLine($"Code: {error.Code}");
            Console.WriteLine($"Path: {string.Join(".", error.Path)}");
            
            // Access extensions for additional details
            if (error.Extensions != null)
            {
                foreach (var ext in error.Extensions)
                {
                    Console.WriteLine($"{ext.Key}: {ext.Value}");
                }
            }
        }
    }
}
```

### Common Error Scenarios

#### Network Errors

```csharp
try
{
    var result = await _client.GetProducts.ExecuteAsync();
    if (!result.IsSuccess)
    {
        // Check for network issues
        Console.WriteLine("Failed to fetch products. Check network connection.");
    }
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Network error: {ex.Message}");
}
```

#### GraphQL Errors

```csharp
var result = await _client.UpdateProduct.ExecuteAsync(productId, input);

if (!result.IsSuccess && result.Errors != null)
{
    foreach (var error in result.Errors)
    {
        if (error.Message.Contains("not found"))
        {
            Console.WriteLine("Product not found");
        }
        else
        {
            Console.WriteLine($"Update failed: {error.Message}");
        }
    }
}
```

## Code Generation

### Automatic Generation

The client code is automatically generated when:
1. The WebApp project is built (generates schema from running server)
2. The GraphQL.Client project is built (generates C# client code from schema)

### Manual Generation

To manually regenerate the client:

```bash
# 1. Start the GraphQL server
dotnet run --project src/MyMarketManager.AppHost

# 2. Generate the schema (StrawberryShake will download it)
dotnet build src/MyMarketManager.GraphQL.Client

# 3. The generated code will be in Generated/MyMarketManagerClient.Client.cs
```

## Testing

### Unit Testing with Mocks

The generated `IMyMarketManagerClient` interface is mockable:

```csharp
public class ProductsViewModelTests
{
    [Fact]
    public async Task LoadProducts_Success_ReturnsProducts()
    {
        // Arrange
        var mockClient = new Mock<IMyMarketManagerClient>();
        var mockResult = new Mock<IOperationResult<IGetProductsResult>>();
        
        mockResult.Setup(r => r.IsSuccess).Returns(true);
        mockResult.Setup(r => r.Data.Products).Returns(new List<Product>
        {
            new Product { Id = Guid.NewGuid(), Name = "Test Product" }
        });
        
        mockClient.Setup(c => c.GetProducts.ExecuteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResult.Object);
        
        var viewModel = new ProductsViewModel(mockClient.Object);
        
        // Act
        var products = await viewModel.LoadProductsAsync();
        
        // Assert
        Assert.Single(products);
        Assert.Equal("Test Product", products[0].Name);
    }
}
```

### Integration Testing

Test against the real GraphQL server:

```csharp
public class GraphQLClientIntegrationTests : IAsyncLifetime
{
    private IMyMarketManagerClient _client;
    
    public async ValueTask InitializeAsync()
    {
        // Start the Aspire app with the GraphQL server
        // Create HttpClient from the app
        // Initialize the GraphQL client
    }
    
    [Fact]
    public async Task CreateProduct_Success()
    {
        var input = new CreateProductInput
        {
            Name = "Integration Test Product",
            Sku = "TEST-001",
            Quality = ProductQuality.Good,
            StockOnHand = 10
        };
        
        var result = await _client.CreateProduct.ExecuteAsync(input);
        
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data?.CreateProduct);
        Assert.Equal("Integration Test Product", result.Data.CreateProduct.Name);
    }
}
```

## Configuration Options

### Custom HttpClient Configuration

```csharp
builder.Services.AddMyMarketManagerClient(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    
    // Add custom headers
    httpClient.DefaultRequestHeaders.Add("X-Custom-Header", "value");
    
    // Set timeout
    httpClient.Timeout = TimeSpan.FromSeconds(30);
    
    // Set base address
    httpClient.BaseAddress = new Uri("https://your-api.com/graphql");
    
    return httpClient;
});
```

### Authentication

For APIs that require authentication:

```csharp
builder.Services.AddMyMarketManagerClient(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    
    // Add Bearer token
    var token = "your-jwt-token";
    httpClient.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);
    
    httpClient.BaseAddress = new Uri("https://your-api.com/graphql");
    
    return httpClient;
});
```

## Platform-Specific Considerations

### MAUI
- Use `HttpClient` with proper TLS configuration
- Handle network connectivity changes
- Consider caching for offline scenarios

### Blazor WASM
- All operations run in the browser
- Subject to CORS policies
- Consider using `PreloadAsync()` for initial data

### Blazor Server
- Operations run on the server
- Can access server-side resources
- Consider SignalR connection limits

## Migration from DbContext

The MyMarketManager WebApp currently uses `DbContext` directly in Razor pages but has the GraphQL client registered. To migrate a page:

**Before (using DbContext):**
```csharp
@inject MyMarketManagerDbContext DbContext

private async Task LoadProducts()
{
    products = await DbContext.Products
        .OrderBy(p => p.Name)
        .ToListAsync();
}
```

**After (using GraphQL Client):**
```csharp
@inject IMyMarketManagerClient GraphQLClient

private async Task LoadProducts()
{
    var result = await GraphQLClient.GetProducts.ExecuteAsync();
    
    if (result.IsSuccess && result.Data?.Products != null)
    {
        products = result.Data.Products.ToList();
    }
}
```

## Troubleshooting

### "Schema not found" Error

Ensure the GraphQL server is running when building the client project. The schema is downloaded at build time.

### Generated Code Not Updated

1. Clean the solution: `dotnet clean`
2. Delete `obj/` and `bin/` folders
3. Rebuild: `dotnet build`

### Network Errors in MAUI

Ensure proper network permissions in platform-specific manifests (Android, iOS).

## Resources

- [StrawberryShake Documentation](https://chillicream.com/docs/strawberryshake)
- [GraphQL Specification](https://spec.graphql.org/)
- [HotChocolate Server Docs](https://chillicream.com/docs/hotchocolate)
