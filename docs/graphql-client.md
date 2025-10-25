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
dotnet graphql download https://localhost:7075/graphql

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

## InMemory Transport for Blazor Server

The MyMarketManager WebApp uses StrawberryShake's InMemory transport for server-side Blazor, providing direct connection to HotChocolate's GraphQL server without HTTP overhead:

```csharp
// Program.cs - Server-side configuration
builder.Services
    .AddMyMarketManagerClient(profile: MyMarketManagerClientProfileKind.InMemory)
    .ConfigureInMemoryClient();
```

This configuration:
- **Zero HTTP overhead** - Direct `IRequestExecutor` connection
- **No URL configuration** - No localhost/port dependencies  
- **Production-ready** - Works in dev, staging, and production
- **Same interface** - `IMyMarketManagerClient` works everywhere

## Migration from DbContext to GraphQL Client

The MyMarketManager WebApp has been successfully migrated from direct DbContext usage to the GraphQL client:

**Before (DbContext):**
```csharp
@inject MyMarketManagerDbContext DbContext

private async Task LoadProducts()
{
    products = await DbContext.Products.OrderBy(p => p.Name).ToListAsync();
}
```

**After (GraphQL Client with InMemory Transport):**
```csharp
@inject IMyMarketManagerClient GraphQLClient
@inject ILogger<Products> Logger

private async Task LoadProducts()
{
    try
    {
        var allProducts = await LoadProductsAsync();
        products = allProducts.OrderBy(p => p.Name).ToList();
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Error loading products");
        errorMessage = $"Error loading products: {ex.Message}";
    }
}

private async Task<List<IGetProducts_Products>> LoadProductsAsync()
{
    var result = await GraphQLClient.GetProducts.ExecuteAsync();
    if (result.Errors != null && result.Errors.Count > 0)
    {
        var errors = string.Join(", ", result.Errors.Select(e => e.Message));
        throw new InvalidOperationException($"GraphQL errors: {errors}");
    }
    return result.Data?.Products?.ToList() ?? new();
}
```

**Key improvements:**
- Inner methods throw exceptions for clean error handling
- Outer methods catch and log errors
- Bootstrap alerts display errors to users (no JavaScript alerts)
- Structured logging with ILogger

## Troubleshooting

### Schema Not Found

If you get a "schema not found" error, download it from the running server:

```bash
# 1. Start the server
dotnet run --project src/MyMarketManager.AppHost

# 2. Download schema and generate client
cd src/MyMarketManager.GraphQL.Client
dotnet graphql download https://localhost:7075/graphql
dotnet graphql generate
```

### Generated Code Not Updated

Regenerate the client after modifying `.graphql` files:

```bash
cd src/MyMarketManager.GraphQL.Client
dotnet graphql generate
```

If schema changes aren't reflected, update the schema first with `dotnet graphql download https://localhost:7075/graphql`.

## Resources

- [StrawberryShake Documentation](https://chillicream.com/docs/strawberryshake)
- [GraphQL Specification](https://spec.graphql.org/)
- [HotChocolate Server Docs](https://chillicream.com/docs/hotchocolate)
