# MyMarketManager.GraphQL.Client

Standalone GraphQL client library for MyMarketManager, compatible with MAUI, Blazor WASM, Blazor Server, and other .NET applications.

## What's Here

- **GraphQL/*.graphql** - GraphQL operation definitions (queries and mutations)
- **Generated/** - Generated client code (do not edit manually)
- **.graphqlrc.json** - StrawberryShake configuration
- **schema.graphql** - Downloaded schema (cached locally, not committed)

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
- **Code Generation** - Client code generated from GraphQL schema using StrawberryShake CLI
- **Dependency Injection** - First-class DI support
- **Error Handling** - Structured error information

## Essential Commands

### Generate Client Code

Client code must be generated manually using the StrawberryShake CLI:

```bash
# 1. Navigate to the client project directory
cd src/MyMarketManager.GraphQL.Client

# 2. (Optional) Download the latest schema from the running app
#    Only needed when the server schema has changed
#    Requires the app to be started first
dotnet graphql update

# 3. Generate the client code
dotnet graphql generate
```

**Note:** Only update the schema when the GraphQL server schema changes. Once downloaded, it's cached locally. See [detailed documentation](../../docs/graphql-client.md#code-generation) for more information.

## Adding New Operations

1. Create/edit `.graphql` file in `GraphQL/` folder
2. Generate the client code: `cd src/MyMarketManager.GraphQL.Client && dotnet graphql generate`
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

After generating, use in code:

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

