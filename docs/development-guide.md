# Development Guide

This guide covers development workflows, tools, and best practices for working with MyMarketManager.

## Prerequisites

Ensure you have the following installed before starting development:

- **.NET 10 SDK** - [Download here](https://dotnet.microsoft.com)
- **Docker Desktop** - Required for SQL Server (Linux) or running the app
- **.NET Aspire Workload**: `dotnet workload install aspire`
- **Git** - For version control
- **Visual Studio 2022** or **Visual Studio Code** (recommended IDEs)
- **SQL Server LocalDB** (Windows only, optional) - Included with Visual Studio or SQL Server Express, used for faster integration tests

**Note on LocalDB:** On Windows, integration tests automatically use LocalDB if available (instant startup). On Linux, tests use Testcontainers with SQL Server. See [Testing Guide](testing.md) for details.

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
- Azurite storage emulator container
- Background blob ingestion service
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

For comprehensive testing documentation, see the **[Testing Guide](testing.md)**.

#### Quick Start

Run all tests:
```bash
dotnet test
```

Run only unit tests (fast):
```bash
dotnet test tests/MyMarketManager.Data.Tests
```

Run only integration tests (requires Aspire DCP):
```bash
dotnet test tests/MyMarketManager.Integration.Tests
```

#### Platform-Specific Testing

Tests use platform-appropriate database provisioning:
- **Windows**: SQL Server LocalDB (instant, no Docker)
- **Linux**: Testcontainers with SQL Server (containerized)

See [Testing Guide - Platform-Specific SQL Server Provisioning](testing.md#platform-specific-sql-server-provisioning) for details.

#### Run Tests by Category

```bash
# GraphQL tests only
dotnet test --filter "Category=GraphQL"

# Skip SSL-requiring tests (useful on Windows)
dotnet test --filter "Requires!=SSL"
```

#### Run Tests with Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Project-Specific Development

### GraphQL Server Development

**Location:** `src/MyMarketManager.WebApp/GraphQL/`

**Workflow:**
1. Add/modify methods in `ProductQueries.cs` or `ProductMutations.cs`
2. Test in Nitro IDE at `/graphql`
3. Schema updates automatically via HotChocolate reflection

Query methods should return `IQueryable<T>` for efficient database queries. Mutation methods should be async and use input records for parameters. See [GraphQL Server documentation](graphql-server.md) for detailed guidance.

### GraphQL Client Development

**Location:** `src/MyMarketManager.GraphQL.Client/`

**Workflow:**
1. Define GraphQL operation in a `.graphql` file in the `GraphQL/` directory
2. Generate typed client code with `dotnet graphql generate`
3. Use the generated operation through `IMyMarketManagerClient`

**Note:** If the server schema has changed, download the updated schema first with `dotnet graphql update`.

See [GraphQL Client documentation](graphql-client.md) and the project's README for detailed instructions.

### Data Layer Development

**Location:** `src/MyMarketManager.Data/`

**Workflow:**
1. Add or modify entity classes in `Entities/`
2. Update `MyMarketManagerDbContext` if adding new `DbSet`
3. Create migration: `dotnet ef migrations add MigrationName --project src/MyMarketManager.Data`
4. Test by running the application (migrations apply automatically)

See [Data Layer documentation](data-layer.md) for entity design best practices and migration management.

### Blob Storage Development

**Location:** `src/MyMarketManager.WebApp/Services/`

**Key Services:**
- `BlobStorageService` - Manages blob operations (upload, download, list)
- `BlobIngestionService` - Background worker that processes new uploads

**Local Development:**
- Azurite emulator runs automatically via Aspire
- Files stored in `supplier-uploads` container
- Background service polls every 5 minutes

**Testing Blob Upload:**

1. Start the application via AppHost
2. Access Azurite through Azure Storage Explorer or REST API
3. Upload a file to the `supplier-uploads` container
4. Background service will detect and create a `StagingBatch` record

**Azurite Connection String:**
```
DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;
```

**Workflow for Adding File Processing Logic:**

1. Modify `BlobIngestionService.ProcessBlobAsync()` method
2. Add ZIP extraction logic (handle password protection)
3. Parse supplier-specific data format
4. Create `StagingPurchaseOrder` and `StagingPurchaseOrderItem` records
5. Test with sample files from blob storage

See [Blob Storage Ingestion](blob-storage-ingestion.md) for architecture details.

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

Enable query logging in `Program.cs` to see generated SQL. See Entity Framework Core documentation for logging configuration options.

### View Aspire Logs

1. Open Aspire Dashboard (URL shown when running AppHost)
2. Click on a resource (e.g., WebApp)
3. View Console, Traces, or Metrics tabs

## Troubleshooting

### Database Connection Issues

**Problem:** SQL Server connection errors during development

**Solutions:**
1. **On Windows**: Ensure LocalDB is installed and running
   - Check: `sqllocaldb info`
   - Start: `sqllocaldb start MSSQLLocalDB`
2. **On Linux**: Ensure Docker Desktop is running
3. Check Aspire Dashboard for connection string details
4. Try restarting Aspire AppHost

**Problem:** Tests fail with database connection errors

**Solution:**
- See [Testing Guide - Troubleshooting](testing.md#troubleshooting) for platform-specific solutions
- Windows: Verify LocalDB installation
- Linux: Verify Docker is running and user has permissions

### Build Issues

**Problem:** "Schema not found" error in GraphQL.Client

**Solution:**
```bash
# Terminal 1: Start server
dotnet run --project src/MyMarketManager.AppHost

# Terminal 2: Update schema and generate client
cd src/MyMarketManager.GraphQL.Client
dotnet graphql update
dotnet graphql generate
```

**Problem:** Migration build errors

**Solution:** 
1. Clean the solution: `dotnet clean`
2. Delete `bin/` and `obj/` folders
3. Rebuild: `dotnet build`

### Runtime Issues

**Problem:** "Cannot connect to database"

**Solution:**
1. Check SQL Server availability:
   - **Windows**: Verify LocalDB is running (`sqllocaldb info`)
   - **Linux**: Check Docker container status in Aspire Dashboard
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
- [Testing Guide](testing.md)
- [GraphQL Server Documentation](graphql-server.md)
- [GraphQL Client Documentation](graphql-client.md)
- [Data Layer Documentation](data-layer.md)
- [Data Model Reference](data-model.md)
- [Blob Storage Ingestion](blob-storage-ingestion.md)
- [Product Requirements](product-requirements.md)
