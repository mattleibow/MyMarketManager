# MyMarketManager.ApiClient Project

This project contains HTTP clients for accessing the MyMarketManager API endpoints.

## Overview

MyMarketManager.ApiClient is a .NET 10 class library that provides typed HTTP clients for accessing the REST API endpoints defined in MyMarketManager.Api. This project can be used by both the web application and future mobile clients to communicate with the API.

## Technology Stack

- **.NET 10.0**: Target framework
- **Microsoft.Extensions.Http**: HTTP client factory support
- **System.Net.Http.Json**: JSON serialization support for HTTP

## Project Structure

```
MyMarketManager.ApiClient/
├── Extensions/
│   └── ServiceCollectionExtensions.cs  # DI registration helpers
├── ProductsClient.cs                   # HTTP client for products API
└── MyMarketManager.ApiClient.csproj
```

## Clients

### ProductsClient

Provides methods for CRUD operations on products:

```csharp
public class ProductsClient
{
    public async Task<List<ProductDto>> GetProductsAsync(string? search = null)
    public async Task<ProductDto?> GetProductAsync(Guid id)
    public async Task<ProductDto?> CreateProductAsync(CreateProductRequest request)
    public async Task<ProductDto?> UpdateProductAsync(Guid id, UpdateProductRequest request)
    public async Task DeleteProductAsync(Guid id)
}
```

## Usage in WebApp

The ProductsClient is registered in the WebApp's `Program.cs`:

```csharp
// Add HttpContextAccessor to get the base URL
builder.Services.AddHttpContextAccessor();

// Configure HttpClient for ProductsClient
builder.Services.AddHttpClient<ProductsClient>((serviceProvider, client) =>
{
    var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
    var httpContext = httpContextAccessor.HttpContext;
    
    if (httpContext != null)
    {
        var request = httpContext.Request;
        client.BaseAddress = new Uri($"{request.Scheme}://{request.Host}");
    }
});
```

Then inject it into Razor components:

```csharp
@inject ProductsClient ProductsClient

@code {
    private async Task LoadProducts()
    {
        var products = await ProductsClient.GetProductsAsync();
    }
}
```

## Usage in Mobile App

For a mobile app, configure the HttpClient with the API's base URL:

```csharp
builder.Services.AddHttpClient<ProductsClient>(client =>
{
    client.BaseAddress = new Uri("https://your-api-url.com");
});
```

## Adding New Clients

When adding a new client:

1. Create the client class (e.g., `SuppliersClient.cs`)
2. Follow the pattern used in `ProductsClient.cs`
3. Add registration helper in `Extensions/ServiceCollectionExtensions.cs` if needed
4. Update this README with the new client information
