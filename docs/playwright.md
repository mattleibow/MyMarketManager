# Playwright Tests

This guide covers Playwright-based end-to-end tests that validate the application UI loads correctly without errors.

## Prerequisites

1. **.NET 10 SDK** (RC or later)
2. **Docker Desktop** (required for SQL Server in Aspire)
3. **Playwright browsers** (Chromium)

## Installing Playwright Browsers

**Important Note:** Building the `MyMarketManager.Integration.Tests` project automatically downloads and installs the Chromium browser via an MSBuild target. This happens automatically when you build the project or run tests.

```bash
# Building the integration tests automatically installs Playwright browsers
dotnet build tests/MyMarketManager.Integration.Tests --configuration Release
```

The MSBuild target runs after the build completes and executes:
```bash
pwsh ./bin/$(Configuration)/$(TargetFramework)/playwright.ps1 install chromium
```

This means that:
- **First build will download browsers** (may take a minute or two)
- **Subsequent builds are faster** (browsers already installed)
- **CI/CD pipelines** automatically get browsers when building the test project

### Manual Installation (Optional)

If you need to manually install or reinstall browsers:

```bash
# Option 1: Using the generated PowerShell script
pwsh tests/MyMarketManager.Integration.Tests/bin/Release/net10.0/playwright.ps1 install chromium

# Option 2: Using Playwright CLI (if installed globally)
playwright install chromium
```

## Running Playwright Tests

Playwright tests are UI tests that:
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

### Run Non-Playwright Tests Only

```bash
dotnet test tests/MyMarketManager.Integration.Tests --filter "FullyQualifiedName~GraphQLEndpointTests"
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

When building the integration tests in GitHub Actions, Playwright browsers are automatically installed by the MSBuild target. However, you may need to install system dependencies:

```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '10.0.x'
    dotnet-quality: 'rc'

- name: Build Integration Tests (installs Playwright browsers automatically)
  run: dotnet build tests/MyMarketManager.Integration.Tests --configuration Release

- name: Install Playwright Browser Dependencies (Linux only)
  run: pwsh tests/MyMarketManager.Integration.Tests/bin/Release/net10.0/playwright.ps1 install-deps chromium

- name: Run Playwright Tests
  run: dotnet test tests/MyMarketManager.Integration.Tests --filter "FullyQualifiedName~PageLoadTests" --no-build
```

**Note:** The `install-deps` command is only needed in CI environments to install system-level dependencies (like fonts, libraries) required by Chromium on Linux.

### Docker Requirement

The Aspire integration tests require Docker to be running for SQL Server. Ensure Docker Desktop is installed and running before executing the tests.

## Troubleshooting

### Error: "Playwright browser is not installed"

**Solution**: This should not happen if you built the project, as browsers are installed automatically. If it does occur, rebuild the integration tests project:
```bash
dotnet build tests/MyMarketManager.Integration.Tests --configuration Release
```

Or manually install:
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

This is expected behavior for end-to-end UI tests.

## Resources

- [Playwright for .NET Documentation](https://playwright.dev/dotnet/)
- [Aspire Testing Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/testing)
- [xUnit Documentation](https://xunit.net/)
