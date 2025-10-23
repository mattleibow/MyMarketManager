# My Market Manager

A mobile and web application for managing weekend market operations — from supplier purchase orders and deliveries to inventory reconciliation, sales imports, and profitability analysis.

## What It Does

MyMarketManager helps market vendors:
- **Track Purchases** - Record purchase orders and deliveries from suppliers
- **Manage Inventory** - Monitor stock levels and product quality
- **Reconcile Sales** - Import sales data and link to inventory via stocktakes
- **Analyze Profitability** - Generate reports on profit margins and sales performance

## Key Features

- **GraphQL API** - Modern, efficient API for flexible data access
- **Cross-Platform Client** - Type-safe GraphQL client for MAUI, Blazor, and .NET apps
- **Cloud-Native** - Built with .NET Aspire for scalable deployment
- **Developer-Friendly** - Comprehensive documentation and built-in tools

## Technology Stack

- **.NET 10** - Latest .NET framework
- **Blazor Server** - Web UI framework
- **Entity Framework Core 9** - Database ORM
- **HotChocolate 15** - GraphQL server
- **StrawberryShake 15** - GraphQL client code generator
- **.NET Aspire** - Cloud-native orchestration
- **SQL Server** - Database

## Quick Start

### Prerequisites

- .NET 10 SDK
- Docker Desktop
- .NET Aspire Workload: `dotnet workload install aspire`

### Run the Application

```bash
dotnet run --project src/MyMarketManager.AppHost
```

This starts the full application stack including SQL Server, applies migrations, and opens the Aspire Dashboard. Access the app at the URL shown in the dashboard (typically `https://localhost:7xxx`).

### Try the GraphQL API

Navigate to `/graphql` to open the Nitro IDE and explore the API interactively.

## Documentation

- **[Project Status](docs/PROJECT_STATUS.md)** - Current implementation status and completion tracking (start here!)
- **[Getting Started](docs/getting-started.md)** - Setup and first steps
- **[Architecture](docs/architecture.md)** - System design and technology choices
- **[Development Guide](docs/development-guide.md)** - Development workflows and best practices
- **[Testing Guide](docs/testing.md)** - Testing infrastructure and platform-specific database provisioning
- **[Data Model](docs/data-model.md)** - Database schema and entities
- **[Product Requirements](docs/product-requirements.md)** - Feature requirements and user stories

### API Documentation

- **[GraphQL Server](docs/graphql-server.md)** - Server implementation and operations
- **[GraphQL Client](docs/graphql-client.md)** - Client library usage and examples
- **[Data Layer](docs/data-layer.md)** - Entity Framework and database management

## Project Structure

- **MyMarketManager.Data** - Data layer with EF Core entities and migrations
- **MyMarketManager.WebApp** - Blazor Server web app with GraphQL API
- **MyMarketManager.GraphQL.Client** - Standalone GraphQL client library
- **MyMarketManager.ServiceDefaults** - Shared .NET Aspire service defaults
- **MyMarketManager.AppHost** - .NET Aspire app host for orchestration
- **MyMarketManager.Tests.Shared** - Shared test infrastructure with platform-specific database helpers
- **MyMarketManager.Data.Tests** - Data layer unit tests
- **MyMarketManager.Integration.Tests** - End-to-end integration tests

## License

This project is licensed under the MIT License.

