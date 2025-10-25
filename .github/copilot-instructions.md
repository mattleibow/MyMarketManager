# My Market Manager - Copilot Instructions

## Repository Overview

My Market Manager is a .NET 10 Blazor Server web application for managing weekend market operations — from supplier purchase orders and deliveries to inventory reconciliation, sales imports, and profitability analysis. The application uses a GraphQL API architecture with HotChocolate (server) and StrawberryShake (client), orchestrated by .NET Aspire for local development.

## Project Structure

The repository is organized into the following projects:

- **src/MyMarketManager.Data** - Data layer with Entity Framework Core 9 entities, DbContext, migrations, and services for SQL Server
- **src/MyMarketManager.WebApp** - Blazor Server web application with integrated GraphQL API server (HotChocolate 15)
- **src/MyMarketManager.GraphQL.Client** - Standalone GraphQL client library (StrawberryShake 15) compatible with MAUI, Blazor WASM, and other .NET applications
- **src/MyMarketManager.ServiceDefaults** - Shared .NET Aspire service defaults for health checks, telemetry, and service discovery
- **src/MyMarketManager.AppHost** - .NET Aspire app host for local development orchestration
- **tests/MyMarketManager.Data.Tests** - Unit tests for data layer using xUnit
- **tests/MyMarketManager.Integration.Tests** - Integration tests using Aspire.Hosting.Testing

## Technology Stack

- **.NET 10 SDK** (RC version 10.0.100-rc.1.25451.107 or later)
- **Blazor Server** - Web UI framework
- **Entity Framework Core 9** - ORM for SQL Server
- **HotChocolate 15** - GraphQL server at `/graphql` endpoint
- **StrawberryShake 15** - Type-safe GraphQL client with code generation
- **.NET Aspire** - Cloud-native orchestration and development tools
- **SQL Server** - Database (containerized via Docker for local development)
- **xUnit** - Testing framework

## Prerequisites for Development

1. .NET 10 SDK (RC or later)
2. Docker Desktop (required for containerized SQL Server)
3. .NET Aspire Workload: `dotnet workload install aspire`

## Build, Test, and Run Commands

### Restore Dependencies

Always restore before building:
```bash
dotnet restore
```

### Build the Solution

Build with Release configuration:
```bash
dotnet build --configuration Release
```

Build time: ~45-50 seconds on first build, faster on subsequent builds.

### Run Tests

Run all tests:
```bash
dotnet test --configuration Release --verbosity normal
```

Test execution time: ~35-40 seconds for unit tests. Integration tests (including Playwright UI tests) may take longer as they require Docker and launch real browsers.

**IMPORTANT**: All tests must always run. Tests are never disabled or excluded by default unless explicitly instructed for a specific reason. Running tests is the only way to ensure the project works correctly.

**CRITICAL**: When running integration tests, you MUST set the environment variable `DCP_IP_VERSION_PREFERENCE=ipv4`. Without this environment variable, all integration tests will fail due to IPv6-related networking issues in .NET Aspire's Developer Control Plane (DCP). Run integration tests with:

```bash
DCP_IP_VERSION_PREFERENCE=ipv4 dotnet test tests/MyMarketManager.Integration.Tests/MyMarketManager.Integration.Tests.csproj
```

Or, if you want to run all the tests:

```bash
DCP_IP_VERSION_PREFERENCE=ipv4 dotnet test
```

This environment variable is essential for the Copilot agent and does not need to be applied to CI/CD pipelines.

### Run the Application

**IMPORTANT**: Always run the application through the Aspire AppHost. Do NOT run `MyMarketManager.WebApp` directly.

```bash
dotnet run --project src/MyMarketManager.AppHost
```

This command:
1. Starts SQL Server in a Docker container
2. Applies EF Core migrations automatically
3. Launches the WebApp with proper configuration
4. Opens the Aspire Dashboard at a localhost URL showing all resources and telemetry

The application will be available at the URL shown in the Aspire Dashboard (typically `https://localhost:7xxx`).

### Database Migrations

The application automatically applies migrations on startup via `DatabaseMigrationService`. Manual migration commands:

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> --project src/MyMarketManager.Data

# Update database (not typically needed as migrations run automatically)
dotnet ef database update --project src/MyMarketManager.Data
```

### GraphQL Client Code Generation

The GraphQL client code is generated manually using the StrawberryShake CLI tools. To regenerate:

```bash
# 1. Navigate to the client project directory
cd src/MyMarketManager.GraphQL.Client

# 2. (Optional) Download the latest schema from the running app
#    Only needed when the server schema has changed
dotnet graphql download https://localhost:7075/graphql

# 3. Generate the client code
dotnet graphql generate
```

**Note**: The schema only needs to be downloaded when the GraphQL server schema changes (new queries, mutations, or types). Once downloaded, it's cached locally.

## Key Configuration Files

- **MyMarketManager.slnx** - Solution file (XML-based, .NET 10+ format)
- **.editorconfig** - Code style and formatting rules (indent: 4 spaces for C#, 2 for XML)
- **.config/dotnet-tools.json** - Local tools manifest (StrawberryShake tools, dotnet-ef)
- **.github/workflows/build.yml** - CI build pipeline
- **.github/workflows/test.yml** - CI test pipeline
- **src/MyMarketManager.Data/Migrations/** - EF Core database migrations
- **src/MyMarketManager.GraphQL.Client/.graphqlrc.json** - GraphQL client generation config

## Architecture Details

### GraphQL Server Implementation

Location: `src/MyMarketManager.WebApp/GraphQL/`

Key files:
- `ProductQueries.cs` - Query operations (getProducts, getProductById)
- `ProductMutations.cs` - Mutation operations (createProduct, updateProduct, deleteProduct)
- Input types: `CreateProductInput`, `UpdateProductInput`

The GraphQL endpoint is at `/graphql` and includes Nitro IDE for development.

### GraphQL Client

Location: `src/MyMarketManager.GraphQL.Client/`

Generated code is in `Generated/` subdirectory. The client provides:
- Type-safe client interface: `IMyMarketManagerClient`
- Dependency injection via `AddMyMarketManagerClient()` extension method
- Generated types from server schema (manually generated using `dotnet graphql generate`)

### Database Context

Location: `src/MyMarketManager.Data/`

Key classes:
- `MyMarketManagerDbContext` - Main DbContext with entities and query filters
- `DbContextMigrator` - Handles migration and seeding
- `DatabaseMigrationService` - Hosted service that runs migrations on startup

The DbContext includes:
- Soft delete query filter (entities with `IsDeleted = true` are excluded by default)
- Audit timestamps (CreatedAt, UpdatedAt)
- Connection string provided by Aspire: configured name is "database"

### .NET Aspire Integration

Location: `src/MyMarketManager.AppHost/Program.cs`

The AppHost orchestrates:
- SQL Server container provisioning
- Service discovery for WebApp
- Health check endpoints at `/health` and `/alive`
- OpenTelemetry exporters for observability

Service defaults are applied via `builder.AddServiceDefaults()` in each project.

## Coding Standards

From `.editorconfig`:
- **Indentation**: 4 spaces for C#, 2 spaces for XML
- **Line endings**: Insert final newline
- **Usings**: Sort system directives first, no separation between groups
- **Type preferences**: Use language keywords (int, string) over BCL types (Int32, String)
- **Accessibility**: Always specify accessibility modifiers for non-interface members
- **Parentheses**: Always add for clarity in arithmetic and relational operations

## Common Development Workflows

### Adding a New Entity

1. Add entity class to `src/MyMarketManager.Data/Entities/`
2. Update `MyMarketManagerDbContext` DbSet
3. Create migration: `dotnet ef migrations add AddXxxEntity --project src/MyMarketManager.Data --startup-project src/MyMarketManager.WebApp`
4. Test migration by running AppHost (auto-applies migrations)

### Adding GraphQL Operations

1. Add query/mutation method to appropriate class in `src/MyMarketManager.WebApp/GraphQL/`
2. Run the application to make schema available
3. Add `.graphql` operation file to `src/MyMarketManager.GraphQL.Client/GraphQL/`
4. Generate client code: `cd src/MyMarketManager.GraphQL.Client && dotnet graphql generate`

### Testing GraphQL API

1. Run AppHost: `dotnet run --project src/MyMarketManager.AppHost`
2. Navigate to `https://localhost:7xxx/graphql` in browser
3. Use Nitro IDE to explore schema and test queries
4. Example operations are in README.md

## Validation and CI/CD

GitHub Actions workflows:
- **build.yml**: Builds solution on push/PR to main
- **test.yml**: Runs tests on push/PR to main

Both workflows:
1. Checkout code
2. Setup .NET 10
3. Build or test with Release configuration

## Important Notes

- **Always run through Aspire AppHost**: Direct execution of WebApp will fail due to missing SQL Server connection
- **Docker is required**: SQL Server runs in a container; ensure Docker Desktop is running
- **Migrations run automatically**: No need to manually run `dotnet ef database update`
- **GraphQL client generation is manual**: Use `dotnet graphql generate` to regenerate client code after schema changes
- **No authentication**: Current implementation has no auth; suitable for local development only
- **.NET 10 is preview**: Using RC SDK version, expect preview warnings in build output

## Additional Documentation

- Main README: `/README.md`
- Documentation Index: `/docs/README.md`
- Getting Started: `/docs/getting-started.md`
- Architecture: `/docs/architecture.md`
- Development Guide: `/docs/development-guide.md`
- GraphQL Server: `/docs/graphql-server.md`
- GraphQL Client: `/docs/graphql-client.md`
- Data Layer: `/docs/data-layer.md`
- Data Model: `/docs/data-model.md`
- Product Requirements: `/docs/product-requirements.md`
