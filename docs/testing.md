# Testing Guide

This guide covers the testing infrastructure, running tests, and writing tests for MyMarketManager.

## Overview

MyMarketManager uses xUnit v3 for testing with a platform-aware approach for integration tests:
- **Windows**: Uses SQL Server LocalDB (instant startup, pre-installed)
- **Linux**: Uses Testcontainers with SQL Server (containerized)
- **Unit Tests**: Use SQLite in-memory databases for speed

## Test Projects

### MyMarketManager.Data.Tests

Unit tests for the data layer.

**Test Types:**
- Entity relationship tests
- Soft delete functionality tests  
- Database operations tests
- DbContext migrator tests

**Database:** Uses SQLite in-memory databases via `SqliteHelper`.

### MyMarketManager.Integration.Tests

End-to-end integration tests using Aspire for orchestration.

**Test Types:**
- GraphQL endpoint tests
- Playwright UI tests (page load validation)
- Full application stack tests
- API contract tests

**Database:** Uses platform-specific SQL Server provisioning via `SqlServerHelper`:
- Windows: LocalDB (instant, no Docker)
- Linux: Testcontainers (containerized SQL Server)

**UI Testing:** Uses Playwright for browser-based end-to-end tests. See [README-Playwright.md](../tests/MyMarketManager.Integration.Tests/README-Playwright.md) for setup instructions.

### MyMarketManager.Tests.Shared

Shared test infrastructure used by both test projects.

**Key Components:**
- `SqlServerHelper`: Platform-aware SQL Server provisioning
- `SqliteHelper`: SQLite in-memory database management
- `TestCategories`: Test categorization (GraphQL, Database, LongRunning)
- `TestRequirements`: Test requirements (SSL)

## Running Tests

### Run All Tests

```bash
dotnet test
```

### Run Specific Test Project

```bash
# Data layer unit tests
dotnet test tests/MyMarketManager.Data.Tests

# Integration tests (requires Aspire DCP)
dotnet test tests/MyMarketManager.Integration.Tests
```

### Run Tests by Category

```bash
# Run only GraphQL tests
dotnet test --filter "Category=GraphQL"

# Run only database tests
dotnet test --filter "Category=Database"

# Exclude long-running tests (Playwright UI tests)
dotnet test --filter "Category!=LongRunning"
```

### Run Tests Excluding Requirements

```bash
# Skip SSL-requiring tests (useful on Windows without certificate trust)
dotnet test --filter "Requires!=SSL"
```

### Run Tests with Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Platform-Specific SQL Server Provisioning

The `SqlServerHelper` class automatically detects the platform and provisions SQL Server appropriately:

### Windows - LocalDB

On Windows, tests use SQL Server LocalDB which is:
- **Pre-installed** with Visual Studio and SQL Server Express
- **Instant startup** - no container overhead
- **Isolated** - each test gets a unique database

**Example usage:**

```csharp
public class MyIntegrationTest(ITestOutputHelper outputHelper) : AppHostTestsBase(outputHelper)
{
    [Fact]
    public async Task MyTest()
    {
        // SqlServerHelper automatically uses LocalDB on Windows
        // Connection string is passed to AppHost
        var response = await WebAppHttpClient.GetAsync("/api/endpoint");
        response.EnsureSuccessStatusCode();
    }
}
```

**LocalDB Connection String Format:**
```
Server=(localdb)\\mssqllocaldb;Database=TestDb_{GUID};Trusted_Connection=true;TrustServerCertificate=true;
```

### Linux - Testcontainers

On Linux, tests use Testcontainers which:
- **Automatically** pulls SQL Server Docker image if needed
- **Starts container** on-demand for each test class
- **Cleans up** after test completion

**Example usage:**

```csharp
public class MyIntegrationTest(ITestOutputHelper outputHelper) : AppHostTestsBase(outputHelper)
{
    [Fact]
    public async Task MyTest()
    {
        // SqlServerHelper automatically uses Testcontainers on Linux
        // Container is started during InitializeAsync
        var response = await WebAppHttpClient.GetAsync("/api/endpoint");
        response.EnsureSuccessStatusCode();
    }
}
```

**Container Configuration:**
- Image: `mcr.microsoft.com/mssql/server:2022-latest`
- Auto-assigned ports to avoid conflicts
- SA password auto-generated

## Writing Tests

### Unit Tests - Data Layer

Use `SqliteTestBase` for fast in-memory database tests:

```csharp
using MyMarketManager.Tests.Shared;

public class ProductTests : SqliteTestBase
{
    [Fact]
    public async Task CanCreateProduct()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product",
            Quality = ProductQuality.Good,
            StockOnHand = 10
        };

        // Act
        Context.Products.Add(product);
        await Context.SaveChangesAsync();

        // Assert
        var saved = await Context.Products.FindAsync(product.Id);
        Assert.NotNull(saved);
        Assert.Equal("Test Product", saved.Name);
    }
}
```

**SqliteTestBase provides:**
- `Context`: Configured `MyMarketManagerDbContext` with SQLite
- `Connection`: Open SQLite connection (stays open for test lifetime)
- Auto-cleanup via `DisposeAsync()`

### Unit Tests - SQL Server Specific

For tests that require SQL Server-specific features:

```csharp
using MyMarketManager.Tests.Shared;

public class SqlServerFeatureTests : SqlServerTestBase
{
    [Fact]
    public async Task TestSqlServerFeature()
    {
        // Uses SqlServerHelper which provides platform-appropriate SQL Server
        // Context is configured with real SQL Server connection
        
        var result = await Context.Database
            .SqlQueryRaw<int>("SELECT 1")
            .ToListAsync();
            
        Assert.Single(result);
    }
}
```

### Integration Tests

Extend `AppHostTestsBase` or `WebAppTestsBase` for integration tests:

```csharp
using MyMarketManager.Tests.Shared;

[Trait(TestCategories.Key, TestCategories.Values.GraphQL)]
[Trait(TestRequirements.Key, TestRequirements.Values.SSL)]
public class MyApiTests(ITestOutputHelper outputHelper) : WebAppTestsBase(outputHelper)
{
    [Fact]
    public async Task ApiEndpoint_ReturnsData()
    {
        // Arrange
        var query = new { query = "{ products { id name } }" };
        var content = new StringContent(
            JsonSerializer.Serialize(query),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await WebAppHttpClient.PostAsync("/graphql", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync();
        Assert.Contains("products", result);
    }
}
```

**Test Traits:**
- `[Trait(TestCategories.Key, TestCategories.Values.GraphQL)]` - Categorizes as GraphQL test
- `[Trait(TestRequirements.Key, TestRequirements.Values.SSL)]` - Requires SSL certificates

**AppHostTestsBase provides:**
- `App`: Running `DistributedApplication` instance
- `Cancel`: Cancellation token from test context
- Auto-cleanup of Aspire app and SQL Server resources

**WebAppTestsBase provides (extends AppHostTestsBase):**
- `WebAppHttpClient`: Configured `HttpClient` for the WebApp
- Helper methods for making requests

### Playwright UI Tests

Playwright tests validate that pages load correctly in a real browser without errors. These tests extend `PlaywrightTestsBase`:

```csharp
using Microsoft.Playwright;
using MyMarketManager.Tests.Shared;
using static Microsoft.Playwright.Assertions;

[Trait(TestCategories.Key, TestCategories.Values.LongRunning)]
public class PageLoadTests(ITestOutputHelper outputHelper) : PlaywrightTestsBase(outputHelper)
{
    [Fact]
    public async Task ProductsPage_LoadsWithoutErrors()
    {
        // Navigate to the page
        await NavigateToAppAsync("/products");

        // Assert page loaded with expected elements
        await Expect(Page!.GetByRole(AriaRole.Heading, new() { Name = "Products" }))
            .ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Add Product" }))
            .ToBeVisibleAsync();
    }
}
```

**Test Traits:**
- `[Trait(TestCategories.Key, TestCategories.Values.LongRunning)]` - Marks as long-running test

**PlaywrightTestsBase provides:**
- `Playwright`: Playwright instance
- `Browser`: Chromium browser (headless)
- `Context`: Browser context with HTTPS error handling
- `Page`: Current page for test interactions
- `NavigateToAppAsync(path)`: Navigate to application routes
- Automatic console error logging
- Auto-cleanup of browser resources

**Prerequisites:**
Playwright tests require browser installation. See [README-Playwright.md](../tests/MyMarketManager.Integration.Tests/README-Playwright.md) for setup:
```bash
pwsh tests/MyMarketManager.Integration.Tests/bin/Release/net10.0/playwright.ps1 install chromium
```

**Running Playwright Tests:**
```bash
# Run all Playwright tests
dotnet test tests/MyMarketManager.Integration.Tests --filter "FullyQualifiedName~PageLoadTests"

# Exclude from fast test runs
dotnet test --filter "Category!=LongRunning"
```

## Test Infrastructure Details

### SqlServerHelper

Located in `MyMarketManager.Tests.Shared`, this helper provides platform-aware SQL Server provisioning:

```csharp
public class SqlServerHelper(ITestOutputHelper outputHelper)
{
    public async Task<string> ConnectAsync()
    {
        if (OperatingSystem.IsWindows())
        {
            // Use LocalDB on Windows
            return $"Server=(localdb)\\mssqllocaldb;Database=TestDb_{Guid.NewGuid():N};...";
        }
        else
        {
            // Use Testcontainers on Linux
            _sqlContainer = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .Build();
            await _sqlContainer.StartAsync();
            return _sqlContainer.GetConnectionString();
        }
    }

    public async Task DisconnectAsync()
    {
        // Cleanup container if using Testcontainers
        if (_sqlContainer is not null)
        {
            await _sqlContainer.DisposeAsync();
        }
    }
}
```

### SqliteHelper

Manages SQLite in-memory databases for unit tests:

```csharp
public class SqliteHelper
{
    public SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }

    public MyMarketManagerDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<MyMarketManagerDbContext>()
            .UseSqlite(connection)
            .Options;
        
        var context = new MyMarketManagerDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
```

### Test Base Classes

**SqliteTestBase:**
```csharp
public abstract class SqliteTestBase : IAsyncLifetime
{
    protected SqliteConnection Connection { get; private set; } = null!;
    protected MyMarketManagerDbContext Context { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var helper = new SqliteHelper();
        Connection = helper.CreateConnection();
        Context = helper.CreateContext(Connection);
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await Connection.DisposeAsync();
    }
}
```

**SqlServerTestBase:**
```csharp
public abstract class SqlServerTestBase(ITestOutputHelper outputHelper) : IAsyncLifetime
{
    private readonly SqlServerHelper _sqlServer = new(outputHelper);
    protected MyMarketManagerDbContext Context { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var connectionString = await _sqlServer.ConnectAsync();
        var options = new DbContextOptionsBuilder<MyMarketManagerDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        Context = new MyMarketManagerDbContext(options);
        await Context.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await _sqlServer.DisconnectAsync();
    }
}
```

## CI/CD Integration

### GitHub Actions Workflow

The test workflow runs on multiple platforms:

```yaml
jobs:
  build:
    runs-on: ${{ matrix.os }}
    strategy:
      fail-fast: false
      matrix:
        os: [ubuntu-latest, windows-latest]
        include:
          - os: windows-latest
            filter: --filter "Requires!=SSL"

    steps:
      - name: Checkout code
        uses: actions/checkout@v5

      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Trust developer certificates
        if: matrix.os == 'ubuntu-latest'
        run: dotnet dev-certs https --trust

      - name: Run tests
        run: dotnet test --configuration Release ${{ matrix.filter }}
```

**Platform-Specific Behavior:**
- **Ubuntu**: Trusts dev certificates, runs all tests
- **Windows**: Excludes SSL tests (certificate trust issues), uses LocalDB

## Troubleshooting

### Windows - LocalDB Issues

**Problem:** "Cannot connect to LocalDB"

**Solution:**
1. Verify LocalDB is installed: `sqllocaldb info`
2. Check LocalDB version: `sqllocaldb versions`
3. Create instance if missing: `sqllocaldb create MSSQLLocalDB`
4. Start instance: `sqllocaldb start MSSQLLocalDB`

**Problem:** "LocalDB database file access denied"

**Solution:**
- Check file permissions on `%LOCALAPPDATA%\Microsoft\Microsoft SQL Server Local DB\Instances`
- Run tests as administrator if needed

### Linux - Testcontainers Issues

**Problem:** "Cannot connect to Docker daemon"

**Solution:**
1. Ensure Docker is installed and running
2. Verify user has Docker permissions: `docker ps`
3. Add user to docker group: `sudo usermod -aG docker $USER` (logout/login required)

**Problem:** "Container startup timeout"

**Solution:**
1. Increase timeout in test configuration
2. Pre-pull image: `docker pull mcr.microsoft.com/mssql/server:2022-latest`
3. Check Docker daemon logs: `journalctl -u docker`

### Integration Test Issues

**Problem:** "Aspire DCP not found"

**Solution:**
1. Install Aspire workload: `dotnet workload install aspire`
2. Verify installation: `dotnet workload list`
3. Update Aspire: `dotnet workload update`

**Problem:** "Certificate trust issues on Windows"

**Solution:**
- Run tests with SSL filter: `dotnet test --filter "Requires!=SSL"`
- Or trust dev certificates: `dotnet dev-certs https --trust`

### Test Isolation Issues

**Problem:** "Tests fail when run together but pass individually"

**Solution:**
1. Ensure each test uses unique database (SqlServerHelper does this automatically)
2. Check for shared static state between tests
3. Verify proper cleanup in `DisposeAsync()`

## Best Practices

### Test Naming

Use descriptive test names that explain the scenario:

```csharp
[Fact]
public async Task CreateProduct_WithValidData_ReturnsCreatedProduct()
{
    // Arrange, Act, Assert
}

[Fact]
public async Task CreateProduct_WithNullName_ThrowsArgumentException()
{
    // Arrange, Act, Assert
}
```

### Test Organization

```csharp
public class ProductTests
{
    public class Creation : SqliteTestBase
    {
        [Fact]
        public async Task WithValidData_Succeeds() { }
        
        [Fact]
        public async Task WithInvalidData_Fails() { }
    }
    
    public class Querying : SqliteTestBase
    {
        [Fact]
        public async Task ById_ReturnsProduct() { }
        
        [Fact]
        public async Task ByName_ReturnsMatchingProducts() { }
    }
}
```

### Async/Await

Always use async methods for database operations:

```csharp
[Fact]
public async Task MyTest()
{
    // Good
    var product = await Context.Products.FindAsync(id);
    
    // Bad - don't use .Result or .Wait()
    // var product = Context.Products.FindAsync(id).Result;
}
```

### Test Data

Use meaningful test data:

```csharp
[Fact]
public async Task CreateProduct_SetsAuditFields()
{
    // Good - clear intent
    var product = new Product
    {
        Name = "Premium Organic Apples",
        Quality = ProductQuality.Excellent,
        StockOnHand = 100
    };
    
    // Bad - meaningless data
    // var product = new Product { Name = "Test", Quality = ProductQuality.Good };
}
```

### Cleanup

Always implement proper cleanup:

```csharp
public class MyTests : IAsyncLifetime
{
    private SqlServerHelper? _sqlServer;
    
    public async ValueTask InitializeAsync()
    {
        _sqlServer = new SqlServerHelper(outputHelper);
        // Setup
    }
    
    public async ValueTask DisposeAsync()
    {
        // Always cleanup even if test fails
        if (_sqlServer is not null)
        {
            await _sqlServer.DisconnectAsync();
        }
    }
}
```

## Resources

- [xUnit Documentation](https://xunit.net/)
- [Testcontainers for .NET](https://dotnet.testcontainers.org/)
- [Playwright for .NET](https://playwright.dev/dotnet/)
- [Aspire Testing Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/testing)
- [SQL Server LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb)
- [Aspire Testing](https://learn.microsoft.com/en-us/dotnet/aspire/testing/)
- [.NET Testing Best Practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
