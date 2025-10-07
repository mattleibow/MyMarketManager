# My Market Manager

My Market Manager is a mobile and web application for managing weekend market operations — from supplier purchase orders and deliveries to inventory reconciliation, sales imports, and profitability analysis.

## Project Structure

- **MyMarketManager.Data** - Data layer with Entity Framework Core entities, DbContext, and migrations for Azure SQL Server
- **MyMarketManager.WebApp** - Blazor web application with integrated GraphQL server
- **MyMarketManager.GraphQL.Client** - Standalone GraphQL client library compatible with MAUI, Blazor WASM, and other .NET applications
- **MyMarketManager.ServiceDefaults** - Shared Aspire service defaults
- **MyMarketManager.AppHost** - .NET Aspire app host for orchestration

## Architecture

MyMarketManager uses a **GraphQL API** architecture powered by [HotChocolate](https://chillicream.com/docs/hotchocolate) and [StrawberryShake](https://chillicream.com/docs/strawberryshake).

### GraphQL Server

The GraphQL server is hosted within MyMarketManager.WebApp at the `/graphql` endpoint. It provides:

- **Strongly-typed schema** based on C# entity classes
- **Efficient data fetching** with precise client-side queries
- **Single endpoint** for all API operations
- **Schema introspection** for tooling support
- **Banana Cake Pop** GraphQL IDE (development mode)

Key Features:
- Query products with flexible filtering
- Create, update, and delete products via mutations
- Direct Entity Framework Core integration
- Type-safe operations with compile-time checking

### GraphQL Client

The MyMarketManager.GraphQL.Client library provides:

- **Type-safe client** generated from GraphQL schema
- **Cross-platform support** for .NET 10, MAUI, Blazor WASM
- **Dependency injection** ready
- **Async/await** patterns throughout

## Getting Started

### Prerequisites

- .NET 10 SDK
- SQL Server or Azure SQL Database
- (Optional) .NET Aspire Workload for orchestration

### Running the Application

```bash
# Run with Aspire (recommended)
dotnet run --project src/MyMarketManager.AppHost

# Or run WebApp directly
dotnet run --project src/MyMarketManager.WebApp
```

The GraphQL API will be available at `https://localhost:5001/graphql`

### Using the GraphQL IDE

In development mode, navigate to `/graphql` in your browser to access Banana Cake Pop, where you can:
- Explore the schema
- Test queries and mutations
- View documentation
- Execute operations with variables

### Example GraphQL Queries

**Get all products:**
```graphql
query {
  products {
    id
    name
    sku
    quality
    stockOnHand
  }
}
```

**Create a product:**
```graphql
mutation {
  createProduct(input: {
    name: "New Product"
    sku: "PROD-001"
    quality: GOOD
    stockOnHand: 100
  }) {
    id
    name
  }
}
```

## Development

### GraphQL Server

Server implementation is in `src/MyMarketManager.WebApp/GraphQL/`:
- `ProductQueries.cs` - Query operations
- `ProductMutations.cs` - Mutation operations
- See [GraphQL Server README](src/MyMarketManager.WebApp/GraphQL/README.md) for detailed documentation

### GraphQL Client

Client library is in `src/MyMarketManager.GraphQL.Client/`:
- Type-safe operations for all GraphQL queries and mutations
- Suitable for MAUI mobile apps and Blazor WASM
- See [GraphQL Client README](src/MyMarketManager.GraphQL.Client/README.md) for usage examples

### Using the Client in a MAUI App

```csharp
// In MauiProgram.cs
using MyMarketManager.GraphQL.Client;

builder.Services.AddMyMarketManagerGraphQLClient(
    "https://your-api-url.com/graphql");

// In your page/view model
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
            // Use result.Data.Products
        }
    }
}
```

## Technologies

- **.NET 10** - Latest .NET framework
- **Blazor** - Web UI framework
- **Entity Framework Core 9** - ORM
- **HotChocolate 15** - GraphQL server
- **StrawberryShake 15** - GraphQL client
- **.NET Aspire** - Cloud-native orchestration
- **SQL Server** - Database

## License

This project is licensed under the MIT License.

