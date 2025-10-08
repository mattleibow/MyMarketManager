# My Market Manager

My Market Manager is a mobile and web application for managing weekend market operations — from supplier purchase orders and deliveries to inventory reconciliation, sales imports, and profitability analysis.

## Project Structure

- **MyMarketManager.Data** - Data layer with Entity Framework Core entities, DbContext, and migrations for SQL Server
- **MyMarketManager.WebApp** - Blazor Server web application with integrated GraphQL API server
- **MyMarketManager.GraphQL.Client** - Standalone GraphQL client library (StrawberryShake) compatible with MAUI, Blazor WASM, and other .NET applications
- **MyMarketManager.ServiceDefaults** - Shared .NET Aspire service defaults
- **MyMarketManager.AppHost** - .NET Aspire app host for local development orchestration
- **MyMarketManager.Integration.Tests** - Integration tests using Aspire.Hosting.Testing

## Architecture

MyMarketManager uses a **GraphQL API** architecture powered by [HotChocolate](https://chillicream.com/docs/hotchocolate) (server) and [StrawberryShake](https://chillicream.com/docs/strawberryshake) (client).

### GraphQL Server (HotChocolate)

The GraphQL server is hosted within MyMarketManager.WebApp at the `/graphql` endpoint. It provides:

- **Strongly-typed schema** based on C# entity classes
- **Efficient data fetching** with precise client-side queries
- **Single endpoint** for all API operations
- **Schema introspection** for tooling and code generation
- **Banana Cake Pop** GraphQL IDE (available at `/graphql` in development mode)

**Current Implementation:**
- `ProductQueries` class with query operations (getProducts, getProductById)
- `ProductMutations` class with mutation operations (createProduct, updateProduct, deleteProduct)
- Direct Entity Framework Core integration via injected `MyMarketManagerDbContext`
- Input types: `CreateProductInput`, `UpdateProductInput`

### GraphQL Client Library (StrawberryShake)

The MyMarketManager.GraphQL.Client library provides:

- **Type-safe client** with generated code from GraphQL schema
- **Cross-platform support** for .NET 10, MAUI, Blazor WASM, Blazor Server
- **Dependency injection** ready with `AddMyMarketManagerClient()` extension
- **Async/await** patterns throughout
- **Generated client interface** `IMyMarketManagerClient` for easy mocking and testing

**Current State:**
- Client code is generated from the running GraphQL server schema
- Located in `src/MyMarketManager.GraphQL.Client/Generated/`
- Ready for use in MAUI mobile apps and other .NET clients
- Currently registered in WebApp but Razor pages still use DbContext directly (migration in progress)

### .NET Aspire Integration

The application uses .NET Aspire for:
- **Local development orchestration** via MyMarketManager.AppHost
- **SQL Server containerization** with automatic database provisioning
- **Service discovery** and configuration management
- **Observability** with built-in health checks and telemetry

## Getting Started

### Prerequisites

- .NET 10 SDK
- Docker Desktop (for containerized SQL Server)
- .NET Aspire Workload: `dotnet workload install aspire`

### Running the Application

**Using Aspire (Recommended):**

```bash
# Run the Aspire AppHost (starts all dependencies including SQL Server)
dotnet run --project src/MyMarketManager.AppHost
```

This will:
1. Start SQL Server in a Docker container
2. Apply EF Core migrations automatically
3. Launch the WebApp with proper configuration
4. Open the Aspire Dashboard showing all resources and telemetry

The application will be available at the URL shown in the Aspire Dashboard (typically `https://localhost:7xxx`).

**Direct WebApp Execution (Not Recommended):**

The WebApp should always be run through the AppHost for proper configuration and dependency management.

### Using the GraphQL IDE

Once the application is running:

1. Navigate to `/graphql` in your browser
2. Banana Cake Pop IDE will open
3. Explore the schema, test queries, and view documentation

### Example GraphQL Operations

**Get all products:**
```graphql
query GetProducts {
  products {
    id
    name
    sku
    quality
    stockOnHand
    description
    notes
    createdAt
    updatedAt
  }
}
```

**Get a specific product:**
```graphql
query GetProduct($id: UUID!) {
  productById(id: $id) {
    id
    name
    sku
    quality
    stockOnHand
    description
    notes
  }
}
```

**Create a product:**
```graphql
mutation CreateProduct {
  createProduct(input: {
    name: "New Product"
    sku: "PROD-001"
    quality: GOOD
    stockOnHand: 100
    description: "A sample product"
  }) {
    id
    name
    sku
  }
}
```

**Update a product:**
```graphql
mutation UpdateProduct($id: UUID!) {
  updateProduct(
    id: $id
    input: {
      name: "Updated Product Name"
      sku: "PROD-001-V2"
      quality: EXCELLENT
      stockOnHand: 150
      description: "Updated description"
    }
  ) {
    id
    name
    sku
    quality
    stockOnHand
  }
}
```

**Delete a product:**
```graphql
mutation DeleteProduct($id: UUID!) {
  deleteProduct(id: $id)
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

