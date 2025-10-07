# MyMarketManager GraphQL Server

This document describes the GraphQL server implementation in MyMarketManager using HotChocolate.

## Overview

The MyMarketManager GraphQL server provides a modern, efficient API for accessing product data. It's built using [HotChocolate](https://chillicream.com/docs/hotchocolate), a powerful GraphQL server for .NET.

## Architecture

### Server Components

```
MyMarketManager.WebApp/
├── GraphQL/
│   ├── ProductQueries.cs      # Query operations
│   ├── ProductMutations.cs    # Mutation operations
│   └── Schemas/
│       └── products.graphql   # GraphQL schema definitions
└── Program.cs                 # Server configuration
```

### Key Features

- **Type-Safe**: Strongly-typed schema based on C# classes
- **Efficient**: Direct database access through Entity Framework Core
- **Single Endpoint**: All operations through `/graphql`
- **Introspection**: Full schema introspection for tooling
- **Banana Cake Pop**: Built-in GraphQL IDE at `/graphql` (development only)

## Configuration

### Program.cs Setup

```csharp
// Add GraphQL server with HotChocolate
builder.Services
    .AddGraphQLServer()
    .AddQueryType<ProductQueries>()
    .AddMutationType<ProductMutations>();

// Map GraphQL endpoint
app.MapGraphQL();
```

### DbContext Integration

The GraphQL resolvers use the injected `MyMarketManagerDbContext`:

```csharp
public class ProductQueries
{
    public IQueryable<Product> GetProducts(MyMarketManagerDbContext context)
    {
        return context.Products.OrderBy(p => p.Name);
    }
}
```

HotChocolate automatically handles:
- Database connection scoping
- Query execution
- Result serialization

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

### Queries

#### Get All Products

```graphql
query GetProducts {
  products {
    id
    sku
    name
    description
    quality
    stockOnHand
  }
}
```

Returns all products, ordered by name.

#### Get Product by ID

```graphql
query GetProductById($id: UUID!) {
  productById(id: $id) {
    id
    sku
    name
    description
    quality
    notes
    stockOnHand
    createdAt
    updatedAt
  }
}
```

Variables:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

### Mutations

#### Create Product

```graphql
mutation CreateProduct($input: CreateProductInputInput!) {
  createProduct(input: $input) {
    id
    name
    sku
    quality
    stockOnHand
  }
}
```

Variables:
```json
{
  "input": {
    "name": "New Product",
    "sku": "PROD-001",
    "quality": "GOOD",
    "stockOnHand": 100,
    "description": "Product description",
    "notes": "Additional notes"
  }
}
```

#### Update Product

```graphql
mutation UpdateProduct($id: UUID!, $input: UpdateProductInputInput!) {
  updateProduct(id: $id, input: $input) {
    id
    name
    sku
    quality
    stockOnHand
    updatedAt
  }
}
```

Variables:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "input": {
    "name": "Updated Product",
    "sku": "PROD-001",
    "quality": "EXCELLENT",
    "stockOnHand": 150
  }
}
```

#### Delete Product

```graphql
mutation DeleteProduct($id: UUID!) {
  deleteProduct(id: $id)
}
```

Variables:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

Returns `true` if successful.

## Development

### Testing with Banana Cake Pop

In development mode, navigate to `/graphql` in your browser to access Banana Cake Pop, the GraphQL IDE.

Features:
- **Schema Explorer**: Browse the complete schema
- **Query Editor**: Write and test queries with autocomplete
- **Variables**: Test with different variable values
- **Documentation**: Inline documentation for all types and fields

### Adding New Operations

1. **Add Query/Mutation Method**

```csharp
public class ProductQueries
{
    public async Task<Product?> GetProductBySkuAsync(
        string sku,
        MyMarketManagerDbContext context,
        CancellationToken cancellationToken)
    {
        return await context.Products
            .FirstOrDefaultAsync(p => p.SKU == sku, cancellationToken);
    }
}
```

2. **Test in Banana Cake Pop**

The new operation is automatically available:

```graphql
query GetProductBySku($sku: String!) {
  productBySku(sku: $sku) {
    id
    name
  }
}
```

### Error Handling

Use `GraphQLException` for errors:

```csharp
if (product == null)
{
    throw new GraphQLException("Product not found");
}
```

Errors are returned in the GraphQL response:

```json
{
  "errors": [
    {
      "message": "Product not found",
      "extensions": {
        "code": "PRODUCT_NOT_FOUND"
      }
    }
  ]
}
```

## Performance

### Database Queries

HotChocolate optimizes database queries:
- **Deferred Execution**: Queries are not executed until needed
- **Projection**: Only requested fields are fetched
- **Batching**: Multiple queries can be batched

### Best Practices

1. **Use IQueryable for Lists**: Return `IQueryable<T>` to allow HotChocolate to optimize
2. **Async Operations**: Use async/await for database operations
3. **Cancellation Tokens**: Support cancellation for long-running operations
4. **Input Validation**: Use data annotations on input types

```csharp
public record CreateProductInput(
    [Required] string Name,
    string? SKU,
    ProductQuality Quality,
    [Range(0, int.MaxValue)] int StockOnHand
);
```

## Security

### Authentication (Future)

Add authentication to the GraphQL server:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<ProductQueries>()
    .AddMutationType<ProductMutations>()
    .AddAuthorization(); // Add authorization

// Then use [Authorize] attribute
public class ProductMutations
{
    [Authorize]
    public async Task<Product> CreateProduct(...)
    {
        // Only authenticated users can create products
    }
}
```

### Rate Limiting (Future)

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<ProductQueries>()
    .AddCostAnalysis(); // Add cost analysis for rate limiting
```

## Monitoring

### Logging

HotChocolate integrates with .NET logging:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<ProductQueries>()
    .AddDiagnosticEventListener<GraphQLLogger>(); // Add custom logger
```

### Metrics

Monitor GraphQL operations:
- Request duration
- Error rates
- Query complexity
- Field execution times

## Troubleshooting

### Common Issues

**Issue**: "Cannot resolve type"
**Solution**: Ensure the type is properly registered in the GraphQL schema

**Issue**: "Database context disposed"
**Solution**: Let HotChocolate manage the DbContext lifecycle - inject it as a parameter

**Issue**: "Circular reference detected"
**Solution**: Use projection to avoid loading navigation properties

## Resources

- [HotChocolate Documentation](https://chillicream.com/docs/hotchocolate)
- [GraphQL Specification](https://spec.graphql.org/)
- [Banana Cake Pop Documentation](https://chillicream.com/docs/bananacakepop)

## License

This project is licensed under the MIT License.
