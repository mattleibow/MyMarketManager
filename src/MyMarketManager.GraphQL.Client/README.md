# MyMarketManager.GraphQL.Client

A standalone GraphQL client library for MyMarketManager, compatible with MAUI mobile apps, Blazor WASM, and other .NET applications.

## Overview

This library provides a strongly-typed GraphQL client for accessing the MyMarketManager GraphQL API. It uses [StrawberryShake](https://chillicream.com/docs/strawberryshake) to generate type-safe client code from GraphQL queries and mutations.

## Features

- **Type-Safe**: All GraphQL operations are strongly typed
- **Cross-Platform**: Works with .NET 10, MAUI, Blazor WASM, ASP.NET Core
- **Auto-Generated**: Client code is automatically generated from GraphQL schema
- **Dependency Injection**: First-class support for Microsoft.Extensions.DependencyInjection
- **Async**: All operations are asynchronous using Task-based async patterns

## Installation

Add the package reference to your project:

```bash
dotnet add reference MyMarketManager.GraphQL.Client
```

Or add directly to your `.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/MyMarketManager.GraphQL.Client/MyMarketManager.GraphQL.Client.csproj" />
</ItemGroup>
```

## Usage

### Blazor WASM

In your `Program.cs`:

```csharp
using MyMarketManager.GraphQL.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add the GraphQL client
builder.Services.AddMyMarketManagerGraphQLClient("https://your-api-url.com/graphql");

await builder.Build().RunAsync();
```

### MAUI

In your `MauiProgram.cs`:

```csharp
using MyMarketManager.GraphQL.Client;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>();

        // Add the GraphQL client
        builder.Services.AddMyMarketManagerGraphQLClient("https://your-api-url.com/graphql");

        return builder.Build();
    }
}
```

### ASP.NET Core / Blazor Server

In your `Program.cs`:

```csharp
using MyMarketManager.GraphQL.Client;

var builder = WebApplication.CreateBuilder(args);

// For local development (same host)
builder.Services.AddMyMarketManagerGraphQLClient((sp, client) =>
{
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var httpContext = httpContextAccessor.HttpContext;
    
    if (httpContext != null)
    {
        var request = httpContext.Request;
        client.BaseAddress = new Uri($"{request.Scheme}://{request.Host}/graphql");
    }
});

// Or for remote server
// builder.Services.AddMyMarketManagerGraphQLClient("https://your-api-url.com/graphql");
```

## Available Operations

### Queries

#### Get All Products

```csharp
public class ProductListPage
{
    private readonly IMyMarketManagerClient _client;

    public ProductListPage(IMyMarketManagerClient client)
    {
        _client = client;
    }

    public async Task LoadProducts()
    {
        var result = await _client.GetProducts.ExecuteAsync();
        
        if (result.IsSuccess && result.Data?.Products != null)
        {
            foreach (var product in result.Data.Products)
            {
                Console.WriteLine($"{product.Name} - {product.Sku}");
            }
        }
    }
}
```

#### Get Product by ID

```csharp
public async Task<ProductDto?> GetProduct(Guid productId)
{
    var result = await _client.GetProductById.ExecuteAsync(productId);
    
    if (result.IsSuccess && result.Data?.ProductById != null)
    {
        return result.Data.ProductById;
    }
    
    return null;
}
```

### Mutations

#### Create Product

```csharp
public async Task<ProductDto?> CreateProduct(string name, string? sku, ProductQuality quality, int stock)
{
    var input = new CreateProductInputInput
    {
        Name = name,
        SKU = sku,
        Quality = quality,
        StockOnHand = stock
    };

    var result = await _client.CreateProduct.ExecuteAsync(input);
    
    if (result.IsSuccess && result.Data?.CreateProduct != null)
    {
        return result.Data.CreateProduct;
    }
    
    return null;
}
```

#### Update Product

```csharp
public async Task<bool> UpdateProduct(Guid id, string name, string? sku, ProductQuality quality, int stock)
{
    var input = new UpdateProductInputInput
    {
        Name = name,
        SKU = sku,
        Quality = quality,
        StockOnHand = stock
    };

    var result = await _client.UpdateProduct.ExecuteAsync(id, input);
    
    return result.IsSuccess;
}
```

#### Delete Product

```csharp
public async Task<bool> DeleteProduct(Guid id)
{
    var result = await _client.DeleteProduct.ExecuteAsync(id);
    
    return result.IsSuccess && result.Data?.DeleteProduct == true;
}
```

## Error Handling

All operations return results that include error information:

```csharp
var result = await _client.GetProducts.ExecuteAsync();

if (result.IsErrorResult())
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error: {error.Message}");
    }
}
else if (result.IsSuccess)
{
    // Handle successful result
    var products = result.Data.Products;
}
```

## Configuration

### Custom HttpClient Configuration

You can provide custom HttpClient configuration:

```csharp
builder.Services.AddMyMarketManagerGraphQLClient((sp, client) =>
{
    client.BaseAddress = new Uri("https://your-api-url.com/graphql");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("X-Custom-Header", "value");
});
```

### Authentication

Add authentication headers:

```csharp
builder.Services.AddMyMarketManagerGraphQLClient((sp, client) =>
{
    client.BaseAddress = new Uri("https://your-api-url.com/graphql");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    // Configure authentication
})
.AddHttpMessageHandler<AuthenticationHandler>(); // Add custom auth handler
```

## Development

### Regenerating Client Code

When the GraphQL schema changes, regenerate the client:

```bash
cd src/MyMarketManager.GraphQL.Client
dotnet graphql update
dotnet graphql generate
```

### GraphQL Queries

All queries and mutations are defined in `GraphQL/products.graphql`. To add new operations:

1. Add your query/mutation to the `.graphql` file
2. Run `dotnet graphql generate`
3. The new operations will be available on the client

## Dependencies

- StrawberryShake.Core 15.1.10
- StrawberryShake.Transport.Http 15.1.10
- Microsoft.Extensions.DependencyInjection.Abstractions 9.0.2

## License

This project is licensed under the MIT License.
