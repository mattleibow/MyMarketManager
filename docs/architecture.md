# Architecture

MyMarketManager uses a modern, cloud-native architecture powered by GraphQL and .NET Aspire.

## Technology Stack

- **.NET 10** - Latest .NET framework
- **Blazor** - Web UI framework
- **Entity Framework Core 9** - ORM for data access
- **HotChocolate 15** - GraphQL server
- **StrawberryShake 15** - GraphQL client
- **.NET Aspire** - Cloud-native orchestration
- **SQL Server** - Database

## Project Structure

The solution is organized into the following projects:

- **MyMarketManager.Data** - Data layer with Entity Framework Core entities, DbContext, and migrations for SQL Server
- **MyMarketManager.WebApp** - Blazor Server web application with integrated GraphQL API server
- **MyMarketManager.GraphQL.Client** - Standalone GraphQL client library (StrawberryShake) compatible with MAUI, Blazor WASM, and other .NET applications
- **MyMarketManager.Scrapers.Core** - Cookie file format and core types for web scraping
- **MyMarketManager.Scrapers** - Web scraping framework and supplier scraper implementations
- **MyMarketManager.SheinCollector** - MAUI app for capturing browser cookies for scraping
- **MyMarketManager.ServiceDefaults** - Shared .NET Aspire service defaults
- **MyMarketManager.AppHost** - .NET Aspire app host for local development orchestration
- **MyMarketManager.Integration.Tests** - Integration tests using Aspire.Hosting.Testing

## GraphQL API Architecture

MyMarketManager uses a GraphQL API architecture to provide efficient, flexible data access for multiple client types.

### GraphQL Server (HotChocolate)

The GraphQL server is hosted within MyMarketManager.WebApp at the `/graphql` endpoint. Key features:

- Strongly-typed schema based on C# entity classes
- Efficient data fetching with precise client-side queries
- Single endpoint for all API operations
- Schema introspection for tooling and code generation
- Nitro IDE available at `/graphql` in development mode

Current operations include product queries (getProducts, getProductById) and mutations (createProduct, updateProduct, deleteProduct). The server is configured in `Program.cs` and operations are defined in the `GraphQL/` directory.

### GraphQL Client Library (StrawberryShake)

The MyMarketManager.GraphQL.Client library provides:

- Type-safe client with generated code from GraphQL schema
- Cross-platform support for .NET 10, MAUI, Blazor WASM, Blazor Server
- Dependency injection ready with `AddMyMarketManagerClient()` extension
- Async/await patterns throughout
- Generated client interface `IMyMarketManagerClient` for easy mocking and testing

Client code is generated from the running GraphQL server schema and located in `src/MyMarketManager.GraphQL.Client/Generated/`. The library is ready for use in MAUI mobile apps and other .NET clients.

## .NET Aspire Integration

The application uses .NET Aspire for:
- **Local development orchestration** via MyMarketManager.AppHost
- **SQL Server containerization** with automatic database provisioning
- **Service discovery** and configuration management
- **Observability** with built-in health checks and telemetry

Aspire manages:
1. Starting SQL Server in Docker
2. Applying database migrations
3. Configuring service endpoints
4. Providing a unified dashboard for monitoring

## Data Layer Architecture

The data layer uses Entity Framework Core 9 with:

- **Code-First approach** - Entities defined as C# classes
- **Migrations** - Database schema versioned and managed through EF Core migrations
- **SQL Server provider** - Compatible with Azure SQL
- **DbContext** - `MyMarketManagerDbContext` provides access to all entities

### Entity Organization

Entities are grouped into two categories:

1. **Core Entities** - Production data (Supplier, PurchaseOrder, Product, Delivery, MarketEvent, ReconciledSale)
2. **Staging Entities** - Import/validation data (StagingBatch, StagingPurchaseOrder, StagingSale)

See [Data Model](data-model.md) for complete entity documentation.

## Background Processing Architecture

The application uses a Channel-based work item processing system for asynchronous background tasks like web scraping and data cleanup.

### Key Components

- **UnifiedBackgroundProcessingService** - Single `BackgroundService` that orchestrates all processors
- **WorkItemProcessingEngine** - Manages handler registrations, coordinates fetch/process cycles
- **IWorkItemHandler<T>** - Interface for handlers that fetch and process work items
- **System.Threading.Channels** - Bounded queues for fair scheduling and starvation prevention

### Benefits

- Single background service replaces multiple independent services
- Fair scheduling prevents any handler from monopolizing resources
- Bounded channels with backpressure control
- Easy to add new processors without creating new services
- Handlers categorized by purpose (Ingestion/Internal/Export) for UI filtering

See [Background Processing](background-processing.md) for detailed architecture and how to create new handlers.

## Web Scraping Architecture

The web scraping system extracts order data from supplier websites for import into the application. It integrates with the background processing system.

### Scraper Projects

- **MyMarketManager.Scrapers.Core** - Cookie file format (`CookieFile`, `CookieData`) for authentication
- **MyMarketManager.Scrapers** - Base scraper framework (`WebScraper`, `IWebScraperSession`) and implementations (e.g., `SheinWebScraper`)
- **MyMarketManager.SheinCollector** - MAUI mobile app for capturing authenticated browser cookies

### Scraping Workflow

1. **Cookie Capture**: MAUI app captures browser cookies during authenticated session
2. **Cookie Submission**: Cookies sent to server and stored in `StagingBatch.FileContents`
3. **Background Processing**: Work item handlers fetch queued batches and process them
4. **Data Extraction**: Scraper fetches order pages and parses HTML/JSON
5. **Staging Storage**: Extracted data stored in `StagingPurchaseOrder` entities
6. **Review & Import**: Staging data reviewed and imported separately

Scrapers use template method pattern - base `WebScraper` class provides orchestration, concrete implementations (e.g., `SheinWebScraper`) handle supplier-specific parsing. Each scraper has a corresponding handler (e.g., `SheinBatchHandler`) that integrates it into the background processing system.

See [Web Scraping](web-scraping.md) and [Background Processing](background-processing.md) for detailed architecture.

## Request Flow

### Web UI Request Flow (Blazor Server)

1. User interacts with Blazor page
2. Page uses `IMyMarketManagerClient` (InMemory transport)
3. StrawberryShake InMemory transport executes query via HotChocolate's `IRequestExecutor`
4. GraphQL resolver accesses DbContext
5. EF Core executes SQL query
6. Results returned through InMemory transport (no HTTP)
7. Blazor updates UI

**Benefits of InMemory Transport:**
- Zero HTTP overhead for server-side Blazor
- No URL/port configuration required
- Direct connection to GraphQL server
- Same `IMyMarketManagerClient` interface as HTTP transport

### External Client Request Flow

1. MAUI/WASM app uses `IMyMarketManagerClient`
2. StrawberryShake sends HTTP POST to `/graphql`
3. HotChocolate processes request
4. GraphQL resolver accesses DbContext
5. EF Core executes SQL query
6. JSON response sent to client
7. StrawberryShake deserializes to typed objects

## Security Considerations

**Current State:** The application has NO AUTHENTICATION and is designed for local development only.

**Future Production Requirements:**
- JWT bearer token authentication
- Role-based authorization on mutations
- Field-level security for sensitive data
- Rate limiting for API endpoints
- Input validation and sanitization

## Scalability Considerations

### Current State (Development)
- Single SQL Server instance
- In-process Blazor Server
- No caching layer

### Future Production Enhancements
- Azure SQL with geo-replication
- Output caching for read-heavy queries
- Redis distributed cache
- SignalR scale-out with backplane
- CDN for static assets

## Deployment Architecture

While not yet implemented, the intended production architecture includes:

- **Azure App Service** - Hosting the Blazor Server WebApp
- **Azure SQL Database** - Production database
- **Azure Container Registry** - Docker images
- **Azure Application Insights** - Telemetry and monitoring
- **Azure Key Vault** - Secrets management

The .NET Aspire configuration will map cleanly to Azure Developer CLI (azd) for deployment.
