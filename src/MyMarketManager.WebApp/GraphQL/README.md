# MyMarketManager GraphQL Server

GraphQL API server implementation using HotChocolate, hosted within the MyMarketManager.WebApp.

## What's Here

- **ProductQueries.cs** - Query operations (getProducts, getProductById)
- **ProductMutations.cs** - Mutation operations and input types (create, update, delete)

## Accessing the API

When running via Aspire AppHost:

```bash
dotnet run --project src/MyMarketManager.AppHost
```

Navigate to `/graphql` to open Nitro IDE for testing and exploring the API.

## Key Features

- **Type-Safe Schema** - Based on C# entity classes
- **EF Core Integration** - Direct database access via injected DbContext
- **Single Endpoint** - All operations at `/graphql`
- **Nitro IDE** - Built-in GraphQL IDE for development
- **Schema Introspection** - Automatic schema discovery for tooling

## Current Operations

### Queries

- `products` - Get all products
- `productById(id: UUID!)` - Get specific product

### Mutations

- `createProduct(input: CreateProductInput!)` - Create new product
- `updateProduct(id: UUID!, input: UpdateProductInput!)` - Update product
- `deleteProduct(id: UUID!)` - Delete product

## Adding New Operations

### Add a Query

In `ProductQueries.cs`:

```csharp
public IQueryable<Product> GetProductsByQuality(
    ProductQuality quality,
    MyMarketManagerDbContext context)
{
    return context.Products.Where(p => p.Quality == quality);
}
```

Query is immediately available in GraphQL:

```graphql
query {
  productsByQuality(quality: EXCELLENT) {
    id
    name
  }
}
```

### Add a Mutation

In `ProductMutations.cs`:

```csharp
public record AdjustStockInput(Guid ProductId, int Adjustment);

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
    await context.SaveChangesAsync(cancellationToken);
    
    return product;
}
```

## Development Workflow

1. **Start the app** via Aspire AppHost
2. **Open Nitro IDE** at `/graphql`
3. **Write and test** queries/mutations
4. **Modify resolvers** in ProductQueries/ProductMutations
5. **Test again** - schema updates automatically

## Technology

- HotChocolate 15 - GraphQL server for .NET
- Entity Framework Core 9 - Database access
- .NET 10

## Documentation

See [GraphQL Server Documentation](../../../docs/graphql-server.md) for detailed information on:
- Complete schema reference
- All available operations
- Error handling patterns
- Performance considerations
- Security best practices
- Extending the API

