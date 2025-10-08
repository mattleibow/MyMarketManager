# MyMarketManager.Data

Data layer for MyMarketManager with Entity Framework Core entities, DbContext, and migrations.

## What's Here

- **Entities/** - EF Core entity classes representing the domain model
- **Enums/** - Enumeration types (ProcessingStatus, CandidateStatus, ProductQuality)
- **Migrations/** - EF Core database migrations for SQL Server
- **MyMarketManagerDbContext.cs** - Main database context

## Essential Commands

### Apply Migrations

```bash
dotnet ef database update --project src/MyMarketManager.Data
```

When running via Aspire (recommended), migrations are applied automatically.

### Create New Migration

```bash
dotnet ef migrations add YourMigrationName --project src/MyMarketManager.Data
```

### View Migration SQL

```bash
dotnet ef migrations script --project src/MyMarketManager.Data
```

## Technology

- .NET 10.0
- Entity Framework Core 9.0
- SQL Server provider (Azure SQL compatible)

## Documentation

See [Data Layer Documentation](../../docs/data-layer.md) for detailed information on:
- Entity overview and relationships
- Working with migrations
- Best practices
- Performance considerations
- Testing strategies

See [Data Model](../../docs/data-model.md) for complete entity field definitions and relationships.
