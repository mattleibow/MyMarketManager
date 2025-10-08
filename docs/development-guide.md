# Development Guide

This guide covers development workflows, tools, and best practices for working with MyMarketManager.

## Prerequisites

Ensure you have the following installed before starting development:

- **.NET 10 SDK** - [Download here](https://dotnet.microsoft.com)
- **Docker Desktop** - Required for SQL Server
- **.NET Aspire Workload**: `dotnet workload install aspire`
- **Git** - For version control
- **Visual Studio 2022** or **Visual Studio Code** (recommended IDEs)

## Development Workflow

### 1. Clone and Setup

```bash
# Clone the repository
git clone https://github.com/mattleibow/MyMarketManager.git
cd MyMarketManager

# Restore dependencies
dotnet restore

# Build the solution
dotnet build
```

### 2. Running the Application

Always use Aspire AppHost for development:

```bash
dotnet run --project src/MyMarketManager.AppHost
```

This starts:
- SQL Server container
- WebApp with GraphQL API
- Aspire Dashboard (monitoring and logs)

Access the app at the URL shown in the Aspire Dashboard (typically `https://localhost:7xxx`).

### 3. Working with the GraphQL API

#### Using Nitro IDE

1. Navigate to `/graphql` in your browser
2. Explore the schema in the Schema Explorer
3. Write queries and mutations in the Operations tab
4. Test with real data

#### Example Development Workflow

1. **Test a query** in Nitro to verify current behavior
2. **Modify the GraphQL resolver** in `ProductQueries.cs` or `ProductMutations.cs`
3. **Refresh Nitro** and re-test
4. **Update client operations** in `src/MyMarketManager.GraphQL.Client/GraphQL/` if needed
5. **Rebuild client** to regenerate typed code

### 4. Working with the Database

#### View Current Migrations

```bash
dotnet ef migrations list --project src/MyMarketManager.Data
```

#### Create a New Migration

After modifying entities:

```bash
dotnet ef migrations add YourMigrationName --project src/MyMarketManager.Data
```

#### Apply Migrations

Migrations are automatically applied when running via Aspire. To manually apply:

```bash
dotnet ef database update --project src/MyMarketManager.Data
```

#### Reset Database

To start fresh:

```bash
dotnet ef database drop --project src/MyMarketManager.Data
dotnet run --project src/MyMarketManager.AppHost
```

### 5. Testing

#### Run All Tests

```bash
dotnet test
```

#### Run Specific Test Project

```bash
dotnet test tests/MyMarketManager.Integration.Tests
```

#### Run Tests with Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Project-Specific Development

### GraphQL Server Development

**Location:** `src/MyMarketManager.WebApp/GraphQL/`

**Key Files:**
- `ProductQueries.cs` - Query operations
- `ProductMutations.cs` - Mutation operations and input types

**Workflow:**
1. Add/modify methods in query or mutation classes
2. Test in Nitro IDE at `/graphql`
3. Schema updates automatically via HotChocolate reflection

**Adding a New Query:**

```csharp
// In ProductQueries.cs
public IQueryable<Product> GetProductsByQuality(
    ProductQuality quality,
    MyMarketManagerDbContext context)
{
    return context.Products.Where(p => p.Quality == quality);
}
```

The query is immediately available in GraphQL:

```graphql
query {
  productsByQuality(quality: EXCELLENT) {
    id
    name
  }
}
```

**Adding a New Mutation:**

```csharp
// In ProductMutations.cs
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

### GraphQL Client Development

**Location:** `src/MyMarketManager.GraphQL.Client/`

**Key Files:**
- `GraphQL/*.graphql` - Operation definitions
- `.graphqlrc.json` - StrawberryShake configuration
- `Generated/` - Auto-generated client code (don't edit manually)

**Workflow:**
1. Define GraphQL operation in `.graphql` file
2. Build the project to generate typed client code
3. Use the generated operation in your app

**Adding a New Operation:**

Create `GraphQL/stock.graphql`:

```graphql
mutation AdjustStock($productId: UUID!, $adjustment: Int!) {
  adjustStock(input: { productId: $productId, adjustment: $adjustment }) {
    id
    stockOnHand
  }
}
```

Build the project:

```bash
dotnet build src/MyMarketManager.GraphQL.Client
```

Use in code:

```csharp
var result = await _client.AdjustStock.ExecuteAsync(productId, adjustment);
```

### Data Layer Development

**Location:** `src/MyMarketManager.Data/`

**Key Files:**
- `Entities/*.cs` - Entity classes
- `Enums/*.cs` - Enumeration types
- `MyMarketManagerDbContext.cs` - DbContext configuration

**Workflow:**
1. Add/modify entity classes
2. Create migration
3. Test migration
4. Apply to database

**Adding a New Entity:**

```csharp
// In Entities/Category.cs
public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    // Navigation properties
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
```

Update DbContext:

```csharp
// In MyMarketManagerDbContext.cs
public DbSet<Category> Categories { get; set; }
```

Create migration:

```bash
dotnet ef migrations add AddCategory --project src/MyMarketManager.Data
```

## Code Style and Standards

### C# Conventions

- Use `PascalCase` for class names and public members
- Use `camelCase` for private fields and local variables
- Use `_camelCase` for private fields (with underscore prefix)
- Use nullable reference types (`string?` for nullable strings)
- Prefer expression-bodied members for simple properties
- Use `var` when type is obvious

### GraphQL Conventions

- Use `PascalCase` for type names
- Use `camelCase` for field names
- Use `SCREAMING_SNAKE_CASE` for enum values
- Add descriptions to types and fields using XML comments

### Git Conventions

- Use meaningful commit messages
- Prefix commits: `feat:`, `fix:`, `docs:`, `refactor:`, `test:`
- Keep commits focused and atomic
- Reference issue numbers when applicable

Example:
```
feat: Add category filtering to products query

Adds a new 'category' parameter to the products query to filter
by category ID.

Closes #123
```

## Debugging

### Debug the WebApp

1. Set `MyMarketManager.AppHost` as startup project
2. Press F5 in Visual Studio
3. Attach debugger to WebApp process from Aspire Dashboard

### Debug GraphQL Operations

1. Set breakpoint in resolver method (e.g., in `ProductQueries.cs`)
2. Execute query in Nitro IDE
3. Debugger will break at your breakpoint

### Debug Database Queries

Enable query logging in `Program.cs`:

```csharp
builder.Services.AddDbContext<MyMarketManagerDbContext>(options =>
{
    options.UseSqlServer(connectionString);
    options.LogTo(Console.WriteLine, LogLevel.Information);
});
```

### View Aspire Logs

1. Open Aspire Dashboard (URL shown when running AppHost)
2. Click on a resource (e.g., WebApp)
3. View Console, Traces, or Metrics tabs

## Troubleshooting

### Docker Container Issues

**Problem:** SQL Server container won't start

**Solutions:**
1. Ensure Docker Desktop is running
2. Check for port conflicts (SQL Server uses 1433)
3. Restart Docker Desktop
4. Check Aspire Dashboard for error messages

### Build Issues

**Problem:** "Schema not found" error in GraphQL.Client

**Solution:** Ensure GraphQL server is running before building client:
```bash
# Terminal 1: Start server
dotnet run --project src/MyMarketManager.AppHost

# Terminal 2: Build client
dotnet build src/MyMarketManager.GraphQL.Client
```

**Problem:** Migration build errors

**Solution:** 
1. Clean the solution: `dotnet clean`
2. Delete `bin/` and `obj/` folders
3. Rebuild: `dotnet build`

### Runtime Issues

**Problem:** "Cannot connect to database"

**Solution:**
1. Check Docker container is running
2. Verify connection string in Aspire Dashboard
3. Try restarting Aspire AppHost

**Problem:** GraphQL schema outdated

**Solution:**
```bash
# Stop all running instances
# Rebuild and restart
dotnet build
dotnet run --project src/MyMarketManager.AppHost
```

## Tools and Extensions

### Recommended Visual Studio Extensions

- **.NET Aspire** - Built-in support for Aspire projects
- **GraphQL** - Syntax highlighting for `.graphql` files
- **Entity Framework Core Power Tools** - Database reverse engineering

### Recommended VS Code Extensions

- **C#** - C# language support
- **GraphQL** - GraphQL syntax and IntelliSense
- **Docker** - Docker container management
- **.NET Aspire** - Aspire project support

### Useful CLI Tools

- **dotnet-ef** - Already installed with EF Core tools
- **dotnet watch** - Auto-rebuild on file changes
- **dotnet aspire** - Aspire CLI commands

## Resources

- [Getting Started Guide](getting-started.md)
- [Architecture Overview](architecture.md)
- [GraphQL Server Documentation](graphql-server.md)
- [GraphQL Client Documentation](graphql-client.md)
- [Data Layer Documentation](data-layer.md)
- [Data Model Reference](data-model.md)
- [Product Requirements](product-requirements.md)
