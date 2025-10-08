# MyMarketManager.GraphQL.Client

Standalone GraphQL client library for MyMarketManager, compatible with MAUI, Blazor WASM, Blazor Server, and other .NET applications.

## What's Here

- **GraphQL/*.graphql** - GraphQL operation definitions (queries and mutations)
- **Generated/** - Auto-generated client code (do not edit manually)
- **.graphqlrc.json** - StrawberryShake configuration
- **schema.graphql** - Downloaded schema (generated at build time, not committed)

## Quick Start

### Add to Your Project

```bash
dotnet add reference path/to/MyMarketManager.GraphQL.Client/MyMarketManager.GraphQL.Client.csproj
```

### Register the Client

**MAUI:**
```csharp
builder.Services.AddMyMarketManagerClient("https://your-api.com/graphql");
```

**Blazor Server (same host):**
```csharp
builder.Services.AddMyMarketManagerClient(); // Uses relative URL
```

### Use the Client

```csharp
public class ProductsViewModel
{
    private readonly IMyMarketManagerClient _client;
    
    public ProductsViewModel(IMyMarketManagerClient client)
    {
        _client = client;
    }
    
    public async Task LoadProducts()
    {
        var result = await _client.GetProducts.ExecuteAsync();
        if (result.IsSuccess)
        {
            var products = result.Data.Products;
        }
    }
}
```

## Key Features

- **Type-Safe** - All operations strongly typed with generated C# classes
- **Cross-Platform** - Works with .NET 10, MAUI, Blazor WASM, Blazor Server
- **Auto-Generated** - Client code generated from GraphQL schema at build time
- **Dependency Injection** - First-class DI support
- **Error Handling** - Structured error information

## Essential Commands

### Regenerate Client Code

```bash
# 1. Start the GraphQL server
dotnet run --project src/MyMarketManager.AppHost

# 2. Build the client (downloads schema and generates code)
dotnet build src/MyMarketManager.GraphQL.Client
```

### Clean Rebuild

If generated code is stale:

```bash
dotnet clean
rm -rf src/MyMarketManager.GraphQL.Client/obj src/MyMarketManager.GraphQL.Client/bin
dotnet build src/MyMarketManager.GraphQL.Client
```

## Adding New Operations

1. Create/edit `.graphql` file in `GraphQL/` folder
2. Build the project
3. Use the generated operation via `IMyMarketManagerClient`

Example:

```graphql
# In GraphQL/products.graphql
query GetProductsByQuality($quality: ProductQuality!) {
  productsByQuality(quality: $quality) {
    id
    name
  }
}
```

After building, use in code:

```csharp
var result = await _client.GetProductsByQuality.ExecuteAsync(ProductQuality.Excellent);
```

## Technology

- .NET 10
- StrawberryShake 15 (GraphQL client code generator)
- Generated from HotChocolate GraphQL server schema

## Documentation

See [GraphQL Client Documentation](../../docs/graphql-client.md) for detailed information on:
- Platform-specific registration (MAUI, Blazor WASM, Blazor Server)
- All available operations (queries and mutations)
- Error handling patterns
- Testing with mocks
- Configuration options
- Authentication setup
- Troubleshooting

