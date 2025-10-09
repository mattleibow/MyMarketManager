# Testing Guide

This document describes the testing infrastructure and best practices for MyMarketManager.

## Test Projects

### MyMarketManager.Data.Tests

Unit tests for the data layer using xUnit.

**Test Types:**
- Entity relationship tests
- Soft delete functionality tests
- Database configuration tests
- DbContext migrator tests

**Database:** Uses either SQLite in-memory databases or Testcontainers.MsSql for SQL Server tests.

**Run:**
```bash
dotnet test tests/MyMarketManager.Data.Tests
```

### MyMarketManager.Integration.Tests

Integration tests using Aspire.Hosting.Testing for end-to-end testing.

**Test Types:**
- GraphQL endpoint tests
- Full application stack tests

**Database:** Configured to use SQLite via `UseSqliteDatabase=True` parameter to avoid SQL Server Docker startup time.

**Run:**
```bash
dotnet test tests/MyMarketManager.Integration.Tests
```

**Note:** Integration tests require the .NET Aspire DCP (Distributed Application Control Plane) to be available on the test machine.

## Running Tests

### Run All Tests

```bash
dotnet test
```

### Run Unit Tests Only (Faster)

Integration tests are marked with `Category=LongRunning` to allow selective execution:

```bash
dotnet test --filter "Category!=LongRunning"
```

This is useful for quick validation during development and in CI/CD pipelines.

### Run Integration Tests Only

```bash
dotnet test --filter "Category=LongRunning"
```

### Run with Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Test Categories

Tests are categorized using xUnit traits:

- **LongRunning** - Integration tests that require the full application stack
- (No category) - Fast unit tests

## SQLite Test Mode

Integration tests use SQLite instead of SQL Server to improve test execution speed:

### How It Works

1. **AppHost Configuration**: The `MyMarketManager.AppHost` project reads the `UseSqliteDatabase` configuration parameter
2. **Conditional Database Setup**: When `UseSqliteDatabase=True`, the AppHost creates a file-based SQLite database instead of starting SQL Server
3. **Schema Creation**: The `DatabaseMigrationService` uses `EnsureCreated()` for SQLite instead of running EF Core migrations
4. **Test Initialization**: The `AppHostTestsBase` passes `UseSqliteDatabase=True` when creating the distributed application

### Benefits

- **Faster startup** - No Docker container to start
- **Simpler infrastructure** - No SQL Server required
- **Same code paths** - Uses the actual AppHost and WebApp projects

### Limitations

- **Requires Aspire DCP** - Integration tests still need the Aspire orchestrator
- **Schema differences** - SQLite and SQL Server have minor differences in supported features
- **EnsureCreated vs Migrations** - SQLite tests don't validate that migrations work correctly

## CI/CD Integration

The GitHub Actions workflows use the test filter to run only fast tests:

```bash
dotnet test --filter "Category!=LongRunning"
```

This ensures quick feedback while avoiding infrastructure dependencies in CI.

## Writing Tests

### Unit Tests

Use `SqliteTestBase` or `SqlServerTestBase` for data layer tests:

```csharp
public class MyEntityTests : SqliteTestBase
{
    [Fact]
    public async Task MyTest()
    {
        // Context is available from the base class
        Context.MyEntities.Add(new MyEntity());
        await Context.SaveChangesAsync();
        
        var count = await Context.MyEntities.CountAsync();
        Assert.Equal(1, count);
    }
}
```

### Integration Tests

Extend `AppHostTestsBase` or `WebAppTestsBase`:

```csharp
public class MyIntegrationTests(ITestOutputHelper outputHelper) 
    : WebAppTestsBase(outputHelper)
{
    [Fact]
    [Trait(TestCategories.Key, TestCategories.Values.LongRunning)]
    public async Task MyTest()
    {
        // WebAppHttpClient is available from the base class
        var response = await WebAppHttpClient.GetAsync("/my-endpoint");
        
        response.EnsureSuccessStatusCode();
    }
}
```

**Important:** Always mark integration tests with `[Trait(TestCategories.Key, TestCategories.Values.LongRunning)]`.

## Troubleshooting

### Integration Tests Timeout

If integration tests timeout, ensure:

1. Docker is running (if using SQL Server mode)
2. The Aspire DCP is available
3. No port conflicts (check Aspire Dashboard port)

### Database Connection Errors

For unit tests:
- Check that the test base class properly initializes the connection
- For SQLite in-memory, ensure the connection stays open

For integration tests:
- Verify the `UseSqliteDatabase` configuration is being passed correctly
- Check logs for database initialization errors

### Test Isolation Issues

Each test should:
- Use a separate database (SqliteTestBase creates a new in-memory DB per test class)
- Clean up any resources in `DisposeAsync()`
- Avoid shared mutable state between tests
