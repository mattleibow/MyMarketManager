# Playwright Tests

This directory contains Playwright-based end-to-end tests that validate the application UI loads correctly without errors.

## Prerequisites

1. **.NET 10 SDK** (RC or later)
2. **Docker Desktop** (required for SQL Server in Aspire)
3. **Playwright browsers** (Chromium)

## Installing Playwright Browsers

Before running the Playwright tests for the first time, you need to install the Chromium browser:

### Option 1: Using PowerShell (Recommended)

```bash
# Build the project first
dotnet build tests/MyMarketManager.Integration.Tests --configuration Release

# Install Chromium browser
pwsh tests/MyMarketManager.Integration.Tests/bin/Release/net10.0/playwright.ps1 install chromium
```

### Option 2: Using Playwright CLI

```bash
# If you have Playwright CLI installed globally
playwright install chromium
```

## Running Playwright Tests

Playwright tests are marked with the `[Trait("Category", "LongRunning")]` attribute because they:
- Start the full Aspire application stack
- Launch a real browser (Chromium)
- Navigate through actual web pages
- Take longer to execute than unit tests

### Run All Integration Tests (Including Playwright)

```bash
dotnet test tests/MyMarketManager.Integration.Tests
```

### Run Only Playwright Page Load Tests

```bash
dotnet test tests/MyMarketManager.Integration.Tests --filter "FullyQualifiedName~PageLoadTests"
```

### Run Non-Playwright Tests Only (Faster)

```bash
dotnet test tests/MyMarketManager.Integration.Tests --filter "FullyQualifiedName~GraphQLEndpointTests"
```

### Exclude Long-Running Tests in CI

```bash
dotnet test --filter "Category!=LongRunning"
```

## Test Structure

### PlaywrightTestsBase

Base class that provides:
- Playwright browser initialization (headless Chromium)
- Browser context with SSL error handling for development certificates
- Page error and console logging
- Automatic cleanup of browser resources
- Navigation helper methods

### PageLoadTests

Tests that verify pages load without errors:

1. **HomePage_LoadsWithoutErrors** - Verifies home page loads successfully
2. **ProductsPage_LoadsWithoutErrors** - Verifies products list page loads with all UI elements
3. **AddProductPage_LoadsWithoutErrors** - Verifies add product form loads correctly
4. **NotFoundPage_LoadsWithoutErrors** - Verifies error page handling
5. **GraphQLEndpoint_IsAccessible** - Verifies GraphQL Nitro IDE is accessible
6. **BrowserConsole_NoUnexpectedErrors** - Verifies no console errors during basic navigation

## Debugging Tests

### View Browser Console Logs

The tests automatically log browser console messages to the test output. To see them:

```bash
dotnet test tests/MyMarketManager.Integration.Tests --filter "FullyQualifiedName~PageLoadTests" --logger "console;verbosity=detailed"
```

### Run with Headed Browser (For Debugging)

To see the browser during test execution, modify `PlaywrightTestsBase.cs`:

```csharp
Browser = await Playwright.Chromium.LaunchAsync(new()
{
    Headless = false,  // Change to false
    SlowMo = 500       // Add slow motion to see actions
});
```

### Take Screenshots on Failure

Add this to your test methods:

```csharp
try
{
    // Your test code
}
catch
{
    await Page.ScreenshotAsync(new() { Path = "failure-screenshot.png" });
    throw;
}
```

## CI/CD Considerations

### GitHub Actions

In GitHub Actions, Playwright tests may require additional setup:

```yaml
- name: Install Playwright Browsers
  run: |
    dotnet build tests/MyMarketManager.Integration.Tests
    pwsh tests/MyMarketManager.Integration.Tests/bin/Release/net10.0/playwright.ps1 install chromium --with-deps

- name: Run Tests
  run: dotnet test tests/MyMarketManager.Integration.Tests --filter "Category=LongRunning"
```

### Docker Requirement

The Aspire integration tests require Docker to be running for SQL Server. Ensure Docker Desktop is installed and running before executing the tests.

## Troubleshooting

### Error: "Playwright browser is not installed"

**Solution**: Run the browser installation command:
```bash
pwsh tests/MyMarketManager.Integration.Tests/bin/Release/net10.0/playwright.ps1 install chromium
```

### Error: "Unable to connect to the application"

**Solution**: Ensure Docker Desktop is running and ports are available for Aspire.

### Error: "SSL certificate errors"

**Solution**: The tests are configured to ignore HTTPS errors for development certificates. This is handled in `PlaywrightTestsBase.cs` with `IgnoreHTTPSErrors = true`.

### Slow Test Execution

Playwright tests are slower than unit tests because they:
- Start the full application stack with Aspire
- Launch a real browser
- Navigate actual web pages
- Wait for network idle states

This is expected behavior. Use the `Category!=LongRunning` filter to exclude them from fast CI runs.

## Resources

- [Playwright for .NET Documentation](https://playwright.dev/dotnet/)
- [Aspire Testing Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/testing)
- [xUnit Documentation](https://xunit.net/)
