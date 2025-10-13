# Getting Started with MyMarketManager

This guide will help you set up and run MyMarketManager for local development.

## Prerequisites

Before you begin, ensure you have the following installed:

- **.NET 10 SDK** - Download from [dotnet.microsoft.com](https://dotnet.microsoft.com)
- **Docker Desktop** - Required for containerized SQL Server
- **.NET Aspire Workload** - Install with: `dotnet workload install aspire`

## Running the Application

### Using Aspire (Recommended)

The recommended way to run MyMarketManager is through the Aspire AppHost, which orchestrates all services:

```bash
# Run the Aspire AppHost (starts all dependencies including SQL Server)
dotnet run --project src/MyMarketManager.AppHost
```

This command will:
1. Start SQL Server in a Docker container
2. Start Azurite (Azure Storage Emulator) in a Docker container
3. Apply EF Core migrations automatically
4. Start background services (database migration, blob ingestion)
5. Launch the WebApp with proper configuration
6. Open the Aspire Dashboard showing all resources and telemetry

The application will be available at the URL shown in the Aspire Dashboard (typically `https://localhost:7xxx`).

### Direct WebApp Execution (Not Recommended)

The WebApp should always be run through the AppHost for proper configuration and dependency management. Running the WebApp directly may result in missing dependencies or incorrect configuration.

## Using the GraphQL IDE

Once the application is running, you can access the GraphQL IDE:

1. Navigate to `/graphql` in your browser (e.g., `https://localhost:7xxx/graphql`)
2. Nitro IDE will open automatically
3. Use the Schema Explorer to browse available types, queries, and mutations
4. Write and execute GraphQL operations in the Operations tab
5. View formatted JSON responses in the Response Viewer

## Example GraphQL Operations

### Get All Products

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

### Get a Specific Product

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

### Create a Product

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

### Update a Product

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

### Delete a Product

```graphql
mutation DeleteProduct($id: UUID!) {
  deleteProduct(id: $id)
}
```

## Next Steps

- Review the [Architecture](architecture.md) to understand the system design
- Read the [Development Guide](development-guide.md) to learn about development workflows
- Explore the [GraphQL Server](graphql-server.md) documentation for API details
- See the [GraphQL Client](graphql-client.md) guide for consuming the API
