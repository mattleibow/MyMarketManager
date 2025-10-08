# MyMarketManager GraphQL Server

This document describes the GraphQL server implementation in MyMarketManager using HotChocolate.

## Overview

The MyMarketManager GraphQL server provides a modern, efficient API for accessing product data. It's built using [HotChocolate 15](https://chillicream.com/docs/hotchocolate), a powerful GraphQL server for .NET.

**Key Features:**
- Type-safe schema based on C# entity classes
- Direct Entity Framework Core integration
- Single `/graphql` endpoint for all operations
- Built-in Banana Cake Pop IDE for development
- Full schema introspection support

## Architecture

### Server Components

```
MyMarketManager.WebApp/
├── GraphQL/
│   ├── ProductQueries.cs      # Query operations
│   ├── ProductMutations.cs    # Mutation operations + Input types
│   └── README.md              # This file
└── Program.cs                 # Server configuration
```

### Configuration (Program.cs)

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

GraphQL resolvers receive the `MyMarketManagerDbContext` via dependency injection:

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
- Database connection scoping per request
- Query execution and optimization
- Result serialization to JSON
- Error handling and reporting

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

**Implementation:**
```csharp
public async Task<Product> CreateProduct(
    CreateProductInput input,
    MyMarketManagerDbContext context,
    CancellationToken cancellationToken)
{
    var product = new Product
    {
        SKU = input.SKU,
        Name = input.Name,
        Description = input.Description,
        Quality = input.Quality,
        Notes = input.Notes,
        StockOnHand = input.StockOnHand
    };

    context.Products.Add(product);
    await context.SaveChangesAsync(cancellationToken);

    return product;
}
```

**Returns:**
- The newly created product with generated ID

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

**Variables:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Implementation:**
```csharp
public async Task<Product> UpdateProduct(
    Guid id,
    UpdateProductInput input,
    MyMarketManagerDbContext context,
    CancellationToken cancellationToken)
{
    var product = await context.Products.FindAsync(new object[] { id }, cancellationToken);
    if (product == null)
    {
        throw new GraphQLException("Product not found");
    }

    product.SKU = input.SKU;
    product.Name = input.Name;
    product.Description = input.Description;
    product.Quality = input.Quality;
    product.Notes = input.Notes;
    product.StockOnHand = input.StockOnHand;

    context.Products.Update(product);
    await context.SaveChangesAsync(cancellationToken);

    return product;
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

**Variables:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Implementation:**
```csharp
public async Task<bool> DeleteProduct(
    Guid id,
    MyMarketManagerDbContext context,
    CancellationToken cancellationToken)
{
    var product = await context.Products.FindAsync(new object[] { id }, cancellationToken);
    if (product == null)
    {
        throw new GraphQLException("Product not found");
    }

    context.Products.Remove(product);
    await context.SaveChangesAsync(cancellationToken);

    return true;
}
```

**Returns:**
- `true` if deletion successful
- Throws `GraphQLException` if product not found

## Development Workflow

### Using Banana Cake Pop IDE

1. Run the application via Aspire:
   ```bash
   dotnet run --project src/MyMarketManager.AppHost
   ```

2. Navigate to `https://localhost:<port>/graphql` (port shown in Aspire Dashboard)

3. The Banana Cake Pop IDE will open, providing:
   - **Schema Explorer**: Browse all types, queries, and mutations
   - **Operations Tab**: Write and execute GraphQL operations
   - **Variables Panel**: Define operation variables
   - **Response Viewer**: View formatted JSON responses
   - **Documentation**: Auto-generated from schema and XML comments

### Testing Queries

Example workflow in Banana Cake Pop:

1. **List all products:**
   - Paste the GetProducts query
   - Click "Run"
   - View formatted results

2. **Create a product:**
   - Paste the CreateProduct mutation
   - Modify the input values
   - Click "Run"
   - Copy the returned ID for next operations

3. **Update the product:**
   - Paste the UpdateProduct mutation
   - Add the ID to variables panel
   - Modify input values
   - Click "Run"

4. **Delete the product:**
   - Paste the DeleteProduct mutation
   - Add the ID to variables
   - Click "Run"

### Schema Introspection

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

## Resources

- [HotChocolate Documentation](https://chillicream.com/docs/hotchocolate)
- [GraphQL Specification](https://spec.graphql.org/)
- [Banana Cake Pop Features](https://chillicream.com/docs/bananacakepop)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)

