# GraphQL Client Documentation

This document describes the GraphQL client library for MyMarketManager, built with StrawberryShake.

## Overview

The MyMarketManager.GraphQL.Client library provides a strongly-typed GraphQL client for accessing the MyMarketManager GraphQL API. It uses [StrawberryShake 15](https://chillicream.com/docs/strawberryshake) to generate type-safe client code from the GraphQL schema.

**Key Features:**
- **Type-Safe**: All GraphQL operations are strongly typed with generated C# classes
- **Cross-Platform**: Works with .NET 10, MAUI, Blazor WASM, Blazor Server, ASP.NET Core
- **Code Generation**: Client code is generated from the GraphQL schema using StrawberryShake CLI
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

The client provides strongly-typed methods for all GraphQL operations:

**Queries:**
- `GetProducts` - Fetch all products
- `GetProductById` - Fetch a specific product by ID

**Mutations:**
- `CreateProduct` - Create a new product
- `UpdateProduct` - Update an existing product
- `DeleteProduct` - Delete a product

All operations are available through the `IMyMarketManagerClient` interface. Each operation returns an `IOperationResult` with strongly-typed data and error information.

For usage examples, see the Quick Start section or [Getting Started guide](getting-started.md).

## Error Handling

StrawberryShake provides structured error handling through the `IOperationResult` interface:

```csharp
var result = await _client.GetProducts.ExecuteAsync();

if (result.IsSuccess)
{
    // Use result.Data
    var products = result.Data.Products;
}
else if (result.Errors != null)
{
    // Handle errors
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error: {error.Message}");
    }
}
```

### Common Error Types

- **Network Errors** - Connection failures, timeouts (catch `HttpRequestException`)
- **GraphQL Errors** - Business logic errors from the server (check `result.Errors`)
- **Validation Errors** - Invalid input data (check `result.Errors` for validation messages)

## Code Generation

The GraphQL client code is generated manually using the StrawberryShake CLI tools.

### When to Generate

Generate client code when:
- Setting up the project for the first time
- After adding or modifying `.graphql` operation files
- After the server schema has changed (requires schema update first)

### Generation Steps

```bash
# 1. Navigate to the client project
cd src/MyMarketManager.GraphQL.Client

# 2. (Optional) Update schema if server changed
#    Requires the app to be running first
dotnet graphql update

# 3. Generate the client code
dotnet graphql generate
```

**Note:** The schema is cached locally after download. Only update it when the GraphQL server schema actually changes (new queries, mutations, or types).

## Testing

### Unit Testing

The `IMyMarketManagerClient` interface is mockable for unit testing. Use a mocking framework like Moq to create test doubles.

### Integration Testing

Integration tests can be written against the real GraphQL server using Aspire.Hosting.Testing. See the `tests/MyMarketManager.Integration.Tests` project for examples.

## Configuration Options

### Custom HttpClient

Configure the underlying HttpClient for authentication, timeouts, and custom headers:

```csharp
builder.Services.AddMyMarketManagerClient(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    httpClient.Timeout = TimeSpan.FromSeconds(30);
    httpClient.DefaultRequestHeaders.Add("X-Custom-Header", "value");
    return httpClient;
});
```

### Authentication

Add JWT bearer tokens or other authentication headers:

```csharp
builder.Services.AddMyMarketManagerClient(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var token = "your-jwt-token";
    httpClient.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);
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

The MyMarketManager WebApp has been successfully migrated to use the GraphQL client instead of direct DbContext usage. All Blazor components now use the GraphQL client for data operations.

**WebApp Components:**
- `Products.razor` - Uses GraphQL client via `IMyMarketManagerClient`
- `ProductForm.razor` - Uses GraphQL client via `IMyMarketManagerClient`

**Example Usage:**
```csharp
@page "/products"
@rendermode InteractiveServer
@using MyMarketManager.GraphQL.Client
@using ProductQuality = MyMarketManager.GraphQL.Client.ProductQuality
@inject IMyMarketManagerClient GraphQLClient
@inject NavigationManager Navigation
@inject IJSRuntime JSRuntime

<PageTitle>Products</PageTitle>

<h1>Products</h1>

@code {
    private List<IGetProducts_Products> products = new();
    private bool loading = true;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadProducts();
    }

    private async Task LoadProducts()
    {
        loading = true;
        errorMessage = null;
        StateHasChanged();
        try
        {
            var result = await GraphQLClient.GetProducts.ExecuteAsync();
            
            if (result.Errors != null && result.Errors.Count > 0)
            {
                errorMessage = "Error loading products: " + string.Join(", ", result.Errors.Select(e => e.Message));
            }
            else if (result.Data?.Products != null)
            {
                products = result.Data.Products.ToList();
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error loading products: {ex.Message}";
        }
        finally
        {
            loading = false;
            StateHasChanged();
        }
    }
}
```

**Benefits of GraphQL Client:**
- **Cross-Platform**: Components work in Blazor Server, WASM, and MAUI
- **Type-Safe**: Compile-time checking of all data operations
- **Testable**: Easy to mock `IMyMarketManagerClient` for unit tests
- **Portable**: Components can be moved to any .NET environment

## Troubleshooting

### Schema Not Found

If you get a "schema not found" error, download it from the running server:

```bash
# 1. Start the server
dotnet run --project src/MyMarketManager.AppHost

# 2. Download schema and generate client
cd src/MyMarketManager.GraphQL.Client
dotnet graphql update
dotnet graphql generate
```

### Generated Code Not Updated

Regenerate the client after modifying `.graphql` files:

```bash
cd src/MyMarketManager.GraphQL.Client
dotnet graphql generate
```

If schema changes aren't reflected, update the schema first with `dotnet graphql update`.

## Resources

- [StrawberryShake Documentation](https://chillicream.com/docs/strawberryshake)
- [GraphQL Specification](https://spec.graphql.org/)
- [HotChocolate Server Docs](https://chillicream.com/docs/hotchocolate)
