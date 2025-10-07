# MyMarketManager.Api Project

This project contains REST API controllers for the MyMarketManager application.

## Overview

MyMarketManager.Api is a .NET 10 class library that contains API controllers for data access operations. The controllers are hosted by the MyMarketManager.WebApp project, allowing the web and future mobile clients to access the same data through REST endpoints.

## Technology Stack

- **.NET 10.0**: Target framework
- **ASP.NET Core**: Web API framework
- **Entity Framework Core**: ORM for data access through MyMarketManager.Data

## Project Structure

```
MyMarketManager.Api/
├── Controllers/           # API controllers
│   └── ProductsController.cs
├── Models/               # DTOs and request/response models
│   ├── ProductDto.cs
│   ├── CreateProductRequest.cs
│   └── UpdateProductRequest.cs
└── MyMarketManager.Api.csproj
```

## API Endpoints

### Products API

**Base URL**: `/api/products`

#### Get All Products
- **GET** `/api/products`
- **Query Parameters**: 
  - `search` (optional): Filter products by name, description, or SKU
- **Response**: Array of `ProductDto`

#### Get Product by ID
- **GET** `/api/products/{id}`
- **Path Parameters**: 
  - `id`: Product GUID
- **Response**: `ProductDto`

#### Create Product
- **POST** `/api/products`
- **Request Body**: `CreateProductRequest`
- **Response**: `ProductDto` (with 201 Created status)

#### Update Product
- **PUT** `/api/products/{id}`
- **Path Parameters**: 
  - `id`: Product GUID
- **Request Body**: `UpdateProductRequest`
- **Response**: `ProductDto`

#### Delete Product
- **DELETE** `/api/products/{id}`
- **Path Parameters**: 
  - `id`: Product GUID
- **Response**: 204 No Content

## Usage

This project is referenced by:
- **MyMarketManager.WebApp**: Hosts the API controllers as part of its endpoints
- **MyMarketManager.ApiClient**: Provides typed HTTP clients to access these endpoints

## Adding New Controllers

When adding a new controller:

1. Create the controller in the `Controllers/` folder
2. Create necessary DTOs in the `Models/` folder
3. Register the controller in the WebApp's `Program.cs` (already done via `AddControllers()`)
4. Create a corresponding client in the ApiClient project
