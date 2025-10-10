# Data Layer Documentation

This document describes the data layer implementation in MyMarketManager, including Entity Framework Core entities, migrations, and database management.

## Overview

The MyMarketManager.Data project is a .NET 10 class library that provides:
- Entity Framework Core entities representing the domain model
- Database context configuration
- EF Core migrations for SQL Server / Azure SQL

## Technology Stack

- **.NET 10.0**: Target framework
- **Entity Framework Core 9.0**: ORM for data access
- **SQL Server**: Database provider (compatible with Azure SQL)

## Project Structure

```
src/MyMarketManager.Data/
├── Entities/              # Entity classes
│   ├── Supplier.cs
│   ├── PurchaseOrder.cs
│   ├── PurchaseOrderItem.cs
│   ├── Product.cs
│   ├── ProductPhoto.cs
│   ├── Delivery.cs
│   ├── DeliveryItem.cs
│   ├── MarketEvent.cs
│   ├── ReconciledSale.cs
│   ├── StagingBatch.cs
│   ├── StagingPurchaseOrder.cs
│   ├── StagingPurchaseOrderItem.cs
│   ├── StagingSale.cs
│   └── StagingSaleItem.cs
├── Enums/                 # Enumeration types
│   ├── ProcessingStatus.cs
│   ├── CandidateStatus.cs
│   └── ProductQuality.cs
├── Migrations/            # EF Core migrations
│   └── 20251006142907_InitialCreate.cs
└── MyMarketManagerDbContext.cs        # Main DbContext
```

## Entity Overview

### Core Entities

These entities represent production data used in day-to-day operations:

- **Supplier** - Vendors from which goods are purchased
- **PurchaseOrder** - Orders placed with suppliers
- **PurchaseOrderItem** - Line items within purchase orders
- **Product** - Catalog items that can be purchased, delivered, and sold
- **ProductPhoto** - Images associated with products
- **Delivery** - Shipments or receipts of goods
- **DeliveryItem** - Individual items received in deliveries
- **MarketEvent** - Market days or events where sales occur
- **ReconciledSale** - Confirmed sales linked to products and market events

### Staging Entities

These entities support data import and validation workflows:

- **StagingBatch** - Supplier or sales data uploads
- **StagingPurchaseOrder** - Parsed supplier orders awaiting validation
- **StagingPurchaseOrderItem** - Line items from supplier orders in staging
- **StagingSale** - Parsed sales awaiting validation
- **StagingSaleItem** - Raw sales data from third-party reports

See [Data Model](data-model.md) for complete entity field definitions and relationships.

## Enumerations

- **ProcessingStatus**: Pending, Partial, Complete
- **CandidateStatus**: Pending, Linked, Ignored
- **ProductQuality**: Excellent, Good, Fair, Poor, Terrible

## Working with Migrations

### Applying Migrations

To apply migrations to your database:

```bash
dotnet ef database update --project src/MyMarketManager.Data
```

When running through Aspire (recommended), migrations are applied automatically on startup.

### Creating New Migrations

When you make changes to entities:

```bash
dotnet ef migrations add YourMigrationName --project src/MyMarketManager.Data
```

This will generate:
- A new migration file in `Migrations/`
- An `Up()` method with SQL to apply changes
- A `Down()` method to revert changes

### Removing a Migration

If you need to remove the last migration (before applying it):

```bash
dotnet ef migrations remove --project src/MyMarketManager.Data
```

### Viewing Migration SQL

To see the SQL that will be executed:

```bash
dotnet ef migrations script --project src/MyMarketManager.Data
```

## DbContext Configuration

The `MyMarketManagerDbContext` is configured in the WebApp's `Program.cs`:

```csharp
// Register DbContext with SQL Server
builder.AddSqlServerDbContext<MyMarketManagerDbContext>("mymarketmanager");
```

The connection string is managed by Aspire and points to:
- Local SQL Server container in development
- Azure SQL Database in production

## Best Practices

### Entity Design

1. **Use value objects** for complex types (e.g., Address, Money)
2. **Keep entities focused** - single responsibility principle
3. **Use navigation properties** for relationships
4. **Add indexes** for frequently queried fields
5. **Use computed columns** sparingly

### Querying

1. **Use IQueryable** to defer execution
2. **Project early** with `.Select()` to reduce data transfer
3. **Use async methods** for all database operations
4. **Avoid N+1 queries** with `.Include()` or projection
5. **Use `.AsNoTracking()` for read-only queries

Example efficient query:

```csharp
// Good: Projects to DTO, no tracking
var products = await context.Products
    .AsNoTracking()
    .Select(p => new ProductDto
    {
        Id = p.Id,
        Name = p.Name,
        StockOnHand = p.StockOnHand
    })
    .ToListAsync();
```

### Change Tracking

1. **Detach entities** when not needed for updates
2. **Use explicit loading** for related data when needed
3. **Be aware of proxy creation** and lazy loading behavior
4. **Use `DbContext.ChangeTracker.Clear()`** to reset context state

### Transactions

For operations requiring multiple saves:

```csharp
using var transaction = await context.Database.BeginTransactionAsync();
try
{
    // Multiple operations
    await context.SaveChangesAsync();
    await context.SaveChangesAsync();
    
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

## Performance Considerations

### Connection Pooling

EF Core uses ADO.NET connection pooling by default. The connection string should not include credentials that change per request.

### DbContext Lifetime

- DbContext is scoped per request in ASP.NET Core
- Don't share DbContext across threads
- Don't cache DbContext instances
- Create new context for long-running operations

### Batch Operations

For bulk operations, consider:
- EF Core's `ExecuteUpdate()` and `ExecuteDelete()`
- Bulk insert libraries for large datasets
- Raw SQL for complex operations

## Testing

For comprehensive testing documentation including platform-specific SQL Server provisioning (LocalDB on Windows, Testcontainers on Linux), see the **[Testing Guide](testing.md)**.

### Quick Examples

#### Unit Tests with SQLite

Use `SqliteTestBase` for fast in-memory database tests:

```csharp
using MyMarketManager.Tests.Shared;

public class ProductTests : SqliteTestBase
{
    [Fact]
    public async Task CanCreateProduct()
    {
        // Context is provided by SqliteTestBase
        var product = new Product { Name = "Test", Quality = ProductQuality.Good };
        Context.Products.Add(product);
        await Context.SaveChangesAsync();
        
        Assert.NotNull(await Context.Products.FindAsync(product.Id));
    }
}
```

#### Integration Tests with SQL Server

Use `SqlServerTestBase` for SQL Server-specific functionality:

```csharp
using MyMarketManager.Tests.Shared;

public class SqlServerFeatureTests : SqlServerTestBase
{
    public SqlServerFeatureTests(ITestOutputHelper outputHelper) 
        : base(outputHelper) { }

    [Fact]
    public async Task TestSqlServerFeature()
    {
        // Context is configured with platform-appropriate SQL Server
        // Windows: LocalDB (instant)
        // Linux: Testcontainers (containerized)
        
        var result = await Context.Database
            .SqlQueryRaw<int>("SELECT 1")
            .ToListAsync();
            
        Assert.Single(result);
    }
}
```

See [Testing Guide - Writing Tests](testing.md#writing-tests) for more examples and best practices.

## Troubleshooting

### Migration Issues

**Problem:** "The migration has already been applied to the database"

**Solution:** Check applied migrations:
```bash
dotnet ef migrations list --project src/MyMarketManager.Data
```

**Problem:** "Pending model changes"

**Solution:** Create a new migration:
```bash
dotnet ef migrations add FixModelChanges --project src/MyMarketManager.Data
```

### Connection Issues

**Problem:** "Cannot connect to SQL Server"

**Solution:** 
1. **Development environment**:
   - **Windows**: Check Docker container status in Aspire Dashboard
   - **Linux**: Ensure Docker Desktop is running
2. **Test environment**:
   - **Windows**: Verify LocalDB is installed: `sqllocaldb info`
   - **Linux**: Verify Docker is running and user has Docker permissions
3. Check connection string in configuration
4. See [Testing Guide - Troubleshooting](testing.md#troubleshooting) for detailed platform-specific solutions

### Performance Issues

**Problem:** Slow queries

**Solution:**
1. Enable query logging: `builder.Services.AddDbContext<MyMarketManagerDbContext>(options => options.LogTo(Console.WriteLine))`
2. Check for N+1 queries
3. Add appropriate indexes
4. Use `.AsNoTracking()` for read-only queries

## Resources

- [Entity Framework Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [EF Core Performance Best Practices](https://learn.microsoft.com/en-us/ef/core/performance/)
- [Data Model Reference](data-model.md)
