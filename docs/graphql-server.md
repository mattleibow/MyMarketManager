# GraphQL Server Documentation

This document describes the GraphQL server implementation in MyMarketManager using HotChocolate.

## Overview

The MyMarketManager GraphQL server provides a modern, efficient API for accessing product data. It's built using [HotChocolate 15](https://chillicream.com/docs/hotchocolate), a powerful GraphQL server for .NET.

**Key Features:**
- Type-safe schema based on C# entity classes
- Direct Entity Framework Core integration
- Single `/graphql` endpoint for all operations
- Built-in Nitro IDE for development
- Full schema introspection support

## Server Components

```
MyMarketManager.WebApp/
├── GraphQL/
│   ├── ProductQueries.cs      # Query operations
│   ├── ProductMutations.cs    # Mutation operations + Input types
│   └── README.md              # Project-specific notes
└── Program.cs                 # Server configuration
```

## Schema

### Types

#### Product

```graphql
type Product {
  id: UUID!
  sku: String
  name: String!
  description: String
  quality: ProductQuality!
  notes: String
  stockOnHand: Int!
  createdAt: DateTime!
  updatedAt: DateTime!
  deletedAt: DateTime
}
```

#### ProductQuality Enum

```graphql
enum ProductQuality {
  EXCELLENT
  GOOD
  FAIR
  POOR
  TERRIBLE
}
```

### Input Types

#### CreateProductInput

```graphql
input CreateProductInput {
  sku: String
  name: String!
  description: String
  quality: ProductQuality!
  notes: String
  stockOnHand: Int!
}
```

#### UpdateProductInput

```graphql
input UpdateProductInput {
  sku: String
  name: String!
  description: String
  quality: ProductQuality!
  notes: String
  stockOnHand: Int!
}
```

## Operations

### Available Operations

The GraphQL server provides the following operations:

**Queries:**
- `products` - Get all products with filtering and sorting support
- `productById(id: UUID!)` - Get a specific product by ID

**Mutations:**
- `createProduct(input: CreateProductInput!)` - Create a new product
- `updateProduct(id: UUID!, input: UpdateProductInput!)` - Update an existing product
- `deleteProduct(id: UUID!)` - Delete a product (soft delete)

For example GraphQL operations and syntax, see the [Getting Started guide](getting-started.md#example-graphql-operations).

## Error Handling

The GraphQL server provides consistent error reporting using HotChocolate's built-in error handling. All errors include:
- Error message
- Source location in the query
- Path to the field that caused the error
- Error code in extensions

Domain-specific errors are thrown as `GraphQLException` in resolver methods. See the implementation in `src/MyMarketManager.WebApp/GraphQL/` for examples.

## Performance Considerations

### Query Optimization

- **Field Selection**: Only requested fields are queried from the database
- **IQueryable**: Queries are not executed until needed
- **Pagination**: Add `@paging` directive for large result sets
- **Filtering**: Add HotChocolate filtering for WHERE clauses
- **Sorting**: Add HotChocolate sorting for ORDER BY

### Connection Pooling

Entity Framework Core manages connection pooling automatically. The `MyMarketManagerDbContext` is scoped per request.

### Caching

For production scenarios, consider:
- Output caching for frequently accessed data
- Redis for distributed caching
- ETags for HTTP-level caching

## Security Considerations

### Current Implementation

The current implementation has **NO AUTHENTICATION** and is intended for local development only.

### Production Requirements

For production deployment, the following security measures should be implemented:

1. **Authentication** - JWT bearer tokens with Identity Provider integration (Azure AD, Auth0, etc.)
2. **Authorization** - Role-based access control using `[Authorize]` attributes and field-level authorization
3. **Rate Limiting** - Query complexity analysis and per-user/IP rate limits
4. **Input Validation** - FluentValidation for complex business rules and format validation

## Extending the API

### Adding New Queries

Add methods to `ProductQueries.cs` in the `src/MyMarketManager.WebApp/GraphQL/` directory. HotChocolate automatically discovers methods and adds them to the schema. Query methods should return `IQueryable<T>` for efficient database queries.

### Adding New Mutations

Add methods to `ProductMutations.cs`. Define input types as C# records for strongly-typed parameters. Mutation methods should be async and return the modified entity or a boolean for success.

## Schema Introspection

The GraphQL schema supports full introspection, which is used by:
- GraphQL IDE tools (Nitro, GraphiQL)
- StrawberryShake client code generation
- API documentation generators

Access introspection through the `/graphql` endpoint's Schema Explorer or via standard GraphQL introspection queries.

## Resources

- [HotChocolate Documentation](https://chillicream.com/docs/hotchocolate)
- [GraphQL Specification](https://spec.graphql.org/)
- [Nitro Features](https://chillicream.com/docs/nitro)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
