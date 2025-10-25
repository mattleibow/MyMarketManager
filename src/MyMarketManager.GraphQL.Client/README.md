# MyMarketManager.GraphQL.Client

Standalone GraphQL client library for MyMarketManager, compatible with MAUI, Blazor WASM, Blazor Server, and other .NET applications.

## Structure

- **GraphQL/*.graphql** - GraphQL operation definitions
- **Generated/** - Generated client code (do not edit manually)
- **.graphqlrc.json** - StrawberryShake configuration

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

## Code Generation

Generate client code manually:

```bash
cd src/MyMarketManager.GraphQL.Client

# Update schema (only if server schema changed)
dotnet graphql download https://localhost:7075/graphql

# Generate client code
dotnet graphql generate
```

## Adding Operations

1. Create/edit `.graphql` file in `GraphQL/` folder
2. Run `dotnet graphql generate`
3. Use the generated operation via `IMyMarketManagerClient`

## Documentation

See [GraphQL Client Documentation](../../docs/graphql-client.md) for:
- Platform-specific registration
- All available operations
- Error handling patterns
- Testing with mocks
- Authentication setup
- Troubleshooting
