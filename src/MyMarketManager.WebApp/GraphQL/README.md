# MyMarketManager GraphQL Server

GraphQL API server implementation using HotChocolate, hosted within the MyMarketManager.WebApp.

## Structure

- **ProductQueries.cs** - Query operations
- **ProductMutations.cs** - Mutation operations and input types

## Quick Start

Run the application via Aspire AppHost:

```bash
dotnet run --project src/MyMarketManager.AppHost
```

Access Nitro IDE at `/graphql` to test queries and mutations.

## Current Operations

**Queries:**
- `products` - Get all products
- `productById(id: UUID!)` - Get specific product

**Mutations:**
- `createProduct(input: CreateProductInput!)` - Create new product
- `updateProduct(id: UUID!, input: UpdateProductInput!)` - Update product
- `deleteProduct(id: UUID!)` - Delete product

## Adding Operations

Add methods to `ProductQueries.cs` or `ProductMutations.cs`. HotChocolate automatically discovers them and adds them to the schema. Test immediately in Nitro IDE.

## Documentation

See [GraphQL Server Documentation](../../../docs/graphql-server.md) for:
- Complete schema reference
- Error handling patterns
- Performance considerations
- Security best practices
- Extending the API
