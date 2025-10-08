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

### Queries

#### Get All Products

**Operation:**
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

**Implementation:**
```csharp
public IQueryable<Product> GetProducts(MyMarketManagerDbContext context)
{
    return context.Products.OrderBy(p => p.Name);
}
```

**Features:**
- Returns all products ordered by name
- HotChocolate automatically applies pagination if requested
- Supports field selection (only requested fields are returned)

#### Get Product By ID

**Operation:**
```graphql
query GetProduct($id: UUID!) {
  productById(id: $id) {
    id
    name
    sku
    quality
    stockOnHand
    description
  }
}
```

**Variables:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Implementation:**
```csharp
public async Task<Product?> GetProductById(
    Guid id,
    MyMarketManagerDbContext context,
    CancellationToken cancellationToken)
{
    return await context.Products.FindAsync(new object[] { id }, cancellationToken);
}
```

**Returns:**
- The product if found
- `null` if not found (serialized as `null` in GraphQL response)

#### Search Products

**Operation:**
```graphql
query SearchProducts($searchTerm: String!) {
  searchProducts(searchTerm: $searchTerm) {
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

**Variables:**
```json
{
  "searchTerm": "widget"
}
```

**Implementation:**
```csharp
public async Task<List<Product>> SearchProducts(
    string searchTerm,
    MyMarketManagerDbContext context,
    CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(searchTerm))
    {
        return await context.Products
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    return await context.Products
        .Where(p => p.Name.Contains(searchTerm) ||
                   (p.Description != null && p.Description.Contains(searchTerm)) ||
                   (p.SKU != null && p.SKU.Contains(searchTerm)))
        .OrderBy(p => p.Name)
        .ToListAsync(cancellationToken);
}
```

**Features:**
- Server-side search filtering by product name, description, or SKU
- Case-sensitive search (SQL Server default)
- Returns all products if search term is empty or whitespace
- Results ordered by name
- More efficient than client-side filtering for large datasets

### Mutations

#### Create Product

**Operation:**
```graphql
mutation CreateProduct {
  createProduct(input: {
    name: "Sample Product"
    sku: "PROD-001"
    quality: GOOD
    stockOnHand: 100
    description: "A sample product for testing"
    notes: "Initial stock"
  }) {
    id
    name
    sku
    quality
    stockOnHand
  }
}
```

**Returns:** The newly created product with generated ID

#### Update Product

**Operation:**
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
      notes: "Stock updated"
    }
  ) {
    id
    name
    sku
    quality
    stockOnHand
    updatedAt
  }
}
```

**Returns:**
- The updated product
- Throws `GraphQLException` if product not found

#### Delete Product

**Operation:**
```graphql
mutation DeleteProduct($id: UUID!) {
  deleteProduct(id: $id)
}
```

**Returns:**
- `true` if deletion successful
- Throws `GraphQLException` if product not found

## Error Handling

### GraphQL Errors

HotChocolate provides consistent error reporting:

```json
{
  "errors": [
    {
      "message": "Product not found",
      "locations": [{"line": 2, "column": 3}],
      "path": ["updateProduct"],
      "extensions": {
        "code": "GRAPHQL_EXCEPTION"
      }
    }
  ],
  "data": null
}
```

### Custom Error Handling

Throw `GraphQLException` for domain errors:

```csharp
if (product == null)
{
    throw new GraphQLException("Product not found");
}
```

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

### Future Enhancements

For production deployment, implement:

1. **Authentication**:
   - Add JWT bearer tokens
   - Integrate with Identity Provider (Azure AD, Auth0, etc.)

2. **Authorization**:
   - Use `[Authorize]` attribute on mutations
   - Implement role-based access control
   - Add field-level authorization

3. **Rate Limiting**:
   - Limit queries per user/IP
   - Implement cost analysis for complex queries

4. **Input Validation**:
   - Add FluentValidation for complex business rules
   - Validate SKU formats, stock quantities, etc.

Example secured mutation:

```csharp
[Authorize(Roles = "Admin")]
public async Task<Product> DeleteProduct(...)
{
    // Implementation
}
```

## Extending the API

### Adding New Queries

1. Add method to `ProductQueries.cs`:
   ```csharp
   public IQueryable<Product> GetProductsBySKU(
       string sku,
       MyMarketManagerDbContext context)
   {
       return context.Products.Where(p => p.SKU == sku);
   }
   ```

2. The query is automatically available:
   ```graphql
   query {
     productsBySKU(sku: "PROD-001") {
       id
       name
     }
   }
   ```

### Adding New Mutations

1. Create input type in `ProductMutations.cs`:
   ```csharp
   public record AdjustStockInput(
       Guid ProductId,
       int Adjustment,
       string Reason
   );
   ```

2. Add mutation method:
   ```csharp
   public async Task<Product> AdjustStock(
       AdjustStockInput input,
       MyMarketManagerDbContext context,
       CancellationToken cancellationToken)
   {
       var product = await context.Products.FindAsync(
           new object[] { input.ProductId }, 
           cancellationToken);
       
       if (product == null)
           throw new GraphQLException("Product not found");
       
       product.StockOnHand += input.Adjustment;
       product.Notes = $"{product.Notes}\n{input.Reason}";
       
       await context.SaveChangesAsync(cancellationToken);
       return product;
   }
   ```

3. Use the mutation:
   ```graphql
   mutation {
     adjustStock(input: {
       productId: "..."
       adjustment: -5
       reason: "Sold at market"
     }) {
       id
       stockOnHand
     }
   }
   ```

## Schema Introspection

The schema is available via introspection:

```graphql
query IntrospectionQuery {
  __schema {
    queryType { name }
    mutationType { name }
    types {
      name
      kind
      description
      fields {
        name
        type {
          name
          kind
        }
      }
    }
  }
}
```

This is used by:
- GraphQL IDE tools
- StrawberryShake client code generation
- API documentation generators

## Resources

- [HotChocolate Documentation](https://chillicream.com/docs/hotchocolate)
- [GraphQL Specification](https://spec.graphql.org/)
- [Nitro Features](https://chillicream.com/docs/nitro)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
