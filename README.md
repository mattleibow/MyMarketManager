# My Market Manager

My Market Manager is a mobile and web application for managing weekend market operations — from supplier purchase orders and deliveries to inventory reconciliation, sales imports, and profitability analysis.

## Project Structure

- **MyMarketManager.Data** - Data layer with Entity Framework Core entities, DbContext, and migrations for Azure SQL Server
- **MyMarketManager.Api** - REST API controllers for data access (class library)
- **MyMarketManager.ApiClient** - HTTP client library for accessing the API endpoints
- **MyMarketManager.WebApp** - Blazor web application that hosts the API controllers and provides the web UI
- **MyMarketManager.AppHost** - .NET Aspire app host for orchestrating the application services
- **MyMarketManager.ServiceDefaults** - Shared Aspire service defaults

## Architecture

The application uses a REST API architecture:

- **MyMarketManager.Api** contains API controllers that provide REST endpoints for CRUD operations on entities
- **MyMarketManager.ApiClient** contains typed HTTP clients that wrap the API endpoints
- **MyMarketManager.WebApp** hosts both the API controllers and the Blazor web UI in a single application
- The Blazor pages use the ApiClient to communicate with the API endpoints

This architecture allows for:
- Separation of concerns between data access and UI
- Ability to build a mobile client that uses the same API
- Easy testing of API endpoints independently
- Consistent data access patterns across web and future mobile clients
