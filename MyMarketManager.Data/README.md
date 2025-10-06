# MyMarketManager.Data Project

This document provides information about the MyMarketManager.Data project that has been created for the MyMarketManager application.

## Project Overview

MyMarketManager.Data is a .NET 10 class library project that contains:
- Entity Framework Core entities
- Database context configuration
- EF Core migrations for Azure SQL Server

## Technology Stack

- **.NET 10.0**: Target framework
- **Entity Framework Core 9.0.9**: ORM for data access
- **SQL Server**: Database provider (compatible with Azure SQL)

## Project Structure

```
MyMarketManager.Data/
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
├── MyMarketManagerDbContext.cs        # Main DbContext
└── MyMarketManagerDbContextFactory.cs # Design-time factory
```

## Entity Overview

### Core Entities
- **Supplier**: Vendors from which goods are purchased
- **PurchaseOrder**: Orders placed with suppliers
- **PurchaseOrderItem**: Line items within purchase orders
- **Product**: Catalog items that can be purchased, delivered, and sold
- **ProductPhoto**: Images associated with products
- **Delivery**: Shipments or receipts of goods
- **DeliveryItem**: Individual items received in deliveries
- **MarketEvent**: Market days or events where sales occur
- **ReconciledSale**: Confirmed sales linked to products and market events

### Staging Entities
- **StagingBatch**: Supplier or sales data uploads
- **StagingPurchaseOrder**: Parsed supplier orders awaiting validation
- **StagingPurchaseOrderItem**: Line items from supplier orders in staging
- **StagingSale**: Parsed sales awaiting validation
- **StagingSaleItem**: Raw sales data from third-party reports

## Enumerations

- **ProcessingStatus**: Pending, Partial, Complete
- **CandidateStatus**: Pending, Linked, Ignored
- **ProductQuality**: Excellent, Good, Fair, Poor, Terrible

## Database Configuration

The DbContext includes configurations for:
- Entity relationships with appropriate delete behaviors
- String length constraints
- Decimal precision for currency fields
- Unique indexes where needed

## Using the Project

### Connection String Configuration

In your application's configuration (e.g., `appsettings.json`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:your-server.database.windows.net,1433;Initial Catalog=MyMarketManager;Persist Security Info=False;User ID=your-username;Password=your-password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
}
```

### Registering the DbContext

In your startup/program configuration:

```csharp
services.AddDbContext<MyMarketManagerDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
```

### Running Migrations

To apply migrations to your database:

```bash
cd MyMarketManager.Data
dotnet ef database update
```

Or from the solution root:

```bash
dotnet ef database update --project MyMarketManager.Data
```

### Creating New Migrations

When you make changes to entities:

```bash
cd MyMarketManager.Data
dotnet ef migrations add YourMigrationName
```

## NuGet Packages

The project includes:
- `Microsoft.EntityFrameworkCore.SqlServer` (9.0.9)
- `Microsoft.EntityFrameworkCore.Design` (9.0.9)

## Notes

- The `MyMarketManagerDbContextFactory` provides design-time support for EF Core tools
- All decimal properties use `decimal(18,2)` precision
- Foreign key relationships use appropriate delete behaviors (Cascade or Restrict)
- The Product.SKU field has a unique index
- All navigation properties are initialized to prevent null reference warnings
