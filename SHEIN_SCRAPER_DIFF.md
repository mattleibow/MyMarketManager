# Shein Scraper Feature - Code Changes Summary

This document summarizes the source code changes made in the `origin/copilot/add-shein-scraper-functionality` branch compared to `main`. This excludes documentation, migration files, and test files.

## Overview

The feature adds web scraping functionality to extract purchase order data from the Shein.com website. The implementation includes:

1. **New Projects**: Two new class library projects for scraper functionality
2. **Data Model Changes**: Enhanced staging entities to support web scraping workflow
3. **Scraper Infrastructure**: Abstract base classes and interfaces for extensible scraper implementations
4. **Shein-Specific Implementation**: Concrete scraper for Shein.com orders
5. **MAUI Cookie Collector**: Enhanced app to export cookies for API use

---

## Project Structure Changes

### Solution File (`MyMarketManager.slnx`)

Added two new projects to the solution:

```xml
<!-- New projects under src folder -->
<Project Path="src/MyMarketManager.Scrapers.Core/MyMarketManager.Scrapers.Core.csproj" />
<Project Path="src/MyMarketManager.Scrapers/MyMarketManager.Scrapers.csproj" />

<!-- New test projects -->
<Project Path="tests/MyMarketManager.Scrapers.Core.Tests/MyMarketManager.Scrapers.Core.Tests.csproj" />
<Project Path="tests/MyMarketManager.Scrapers.Tests/MyMarketManager.Scrapers.Tests.csproj" />
```

### .gitignore

Added entries for macOS and development scripts:

```
.DS_Store
*.dev.ps1
```

---

## 1. New Project: MyMarketManager.Scrapers.Core

A lightweight class library containing shared data models for web scraping, independent of Entity Framework or other dependencies.

### `CookieData.cs` (New File)

Represents a single HTTP cookie with all standard properties:

```csharp
public class CookieData
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public string? Path { get; set; }
    public bool Secure { get; set; }
    public bool HttpOnly { get; set; }
    public DateTimeOffset? Expires { get; set; }
    public string? SameSite { get; set; }
}
```

### `CookieFile.cs` (New File)

Represents a cookie file format for web scraping sessions that can be serialized and stored:

```csharp
public class CookieFile
{
    public string Domain { get; set; } = string.Empty;
    public DateTimeOffset CapturedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public Dictionary<string, CookieData> Cookies { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
}
```

### `MyMarketManager.Scrapers.Core.csproj` (New File)

Simple .NET 10 class library with no external dependencies:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

---

## 2. New Project: MyMarketManager.Scrapers

Core scraping infrastructure with abstract base classes and Shein-specific implementation.

### `MyMarketManager.Scrapers.csproj` (New File)

References both Scrapers.Core and Data projects:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\MyMarketManager.Scrapers.Core\MyMarketManager.Scrapers.Core.csproj" />
    <ProjectReference Include="..\MyMarketManager.Data\MyMarketManager.Data.csproj" />
  </ItemGroup>
</Project>
```

### `ScraperConfiguration.cs` (New File)

Configuration for HTTP settings and scraping behavior:

```csharp
public class ScraperConfiguration
{
    public string UserAgent { get; set; } = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36";
    public Dictionary<string, string> AdditionalHeaders { get; set; } = new();
    public TimeSpan RequestDelay { get; set; } = TimeSpan.FromSeconds(1);
    public int MaxConcurrentRequests { get; set; } = 1;
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
```

### `IWebScraperSession.cs` (New File)

Interface for HTTP session management:

```csharp
public interface IWebScraperSession : IDisposable
{
    Task<string> FetchPageAsync(string url, CancellationToken cancellationToken = default);
}
```

### `IWebScraperSessionFactory.cs` (New File)

Factory for creating scraper sessions with cookies:

```csharp
public interface IWebScraperSessionFactory
{
    IWebScraperSession CreateSession(CookieFile cookies);
}
```

### `WebScraperSession.cs` (New File)

Default implementation using HttpClient:

```csharp
public class WebScraperSession(HttpClient httpClient, ILogger logger) : IWebScraperSession
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger _logger = logger;
    private bool _disposed;

    public async Task<string> FetchPageAsync(string url, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _logger.LogDebug("Fetching from {Url}", url);
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _httpClient?.Dispose();
        _disposed = true;
    }
}
```

### `WebScraperSessionFactory.cs` (New File)

Creates HTTP clients with configured cookies and headers:

```csharp
public class WebScraperSessionFactory(
    ILogger<WebScraperSessionFactory> logger,
    IOptions<ScraperConfiguration> configuration) : IWebScraperSessionFactory
{
    private readonly ILogger<WebScraperSessionFactory> _logger = logger;
    private readonly ScraperConfiguration _configuration = configuration.Value;

    public IWebScraperSession CreateSession(CookieFile cookies)
    {
        var httpClient = CreateHttpClient(cookies);
        return new WebScraperSession(httpClient, _logger);
    }

    protected virtual HttpClient CreateHttpClient(CookieFile cookies)
    {
        var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new CookieContainer()
        };

        // Add cookies to the container
        foreach (var cookie in cookies.Cookies.Values)
        {
            try
            {
                handler.CookieContainer.Add(new Uri($"https://{cookies.Domain}"), new Cookie
                {
                    Name = cookie.Name,
                    Value = cookie.Value,
                    Domain = cookie.Domain ?? $".{cookies.Domain}",
                    Path = cookie.Path ?? "/",
                    Secure = cookie.Secure,
                    HttpOnly = cookie.HttpOnly
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to add cookie {CookieName}", cookie.Name);
            }
        }

        var client = new HttpClient(handler);
        client.Timeout = _configuration.RequestTimeout;
        client.DefaultRequestHeaders.Add("user-agent", _configuration.UserAgent);
        
        foreach (var header in _configuration.AdditionalHeaders)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
        }

        return client;
    }
}
```

### `WebScraperOrderSummary.cs` (New File)

Represents order summary from list page:

```csharp
public class WebScraperOrderSummary : Dictionary<string, string>
{
    public string? RawData { get; set; }
}
```

### `WebScraperOrder.cs` (New File)

Represents detailed order information:

```csharp
public class WebScraperOrder(WebScraperOrderSummary orderSummary) : Dictionary<string, string>
{
    public WebScraperOrderSummary OrderSummary { get; } = orderSummary;
    public List<WebScraperOrderItem> OrderItems { get; set; } = new();
    public string? RawData { get; set; }
}
```

### `WebScraperOrderItem.cs` (New File)

Represents a single order line item:

```csharp
public class WebScraperOrderItem(WebScraperOrder order) : Dictionary<string, string>
{
    public WebScraperOrder Order { get; } = order;
    public string? RawData { get; set; }
}
```

### `WebScraper.cs` (New File)

Abstract base class providing the scraping orchestration logic:

```csharp
public abstract class WebScraper(
    MyMarketManagerDbContext context,
    ILogger logger,
    IOptions<ScraperConfiguration> configuration,
    IWebScraperSessionFactory sessionFactory)
{
    protected MyMarketManagerDbContext Context { get; } = context;
    protected ILogger Logger { get; } = logger;
    protected ScraperConfiguration Configuration { get; } = configuration.Value;
    protected IWebScraperSessionFactory SessionFactory { get; } = sessionFactory;

    // Abstract methods that scrapers must implement
    public abstract string GetOrdersListUrl();
    public abstract string GetOrderDetailUrl(WebScraperOrderSummary order);
    public abstract IAsyncEnumerable<WebScraperOrderSummary> ParseOrdersListAsync(string ordersListHtml, CancellationToken cancellationToken);
    public abstract Task<WebScraperOrder> ParseOrderDetailsAsync(string orderDetailHtml, WebScraperOrderSummary orderSummary, CancellationToken cancellationToken);
    public abstract Task UpdateStagingPurchaseOrderAsync(StagingPurchaseOrder stagingOrder, WebScraperOrder order, CancellationToken cancellationToken);

    // Main entry point
    public async Task StartScrapingAsync(Guid supplierId, CookieFile? cookies, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Starting scraping for supplier {SupplierId}", supplierId);
        
        var batchId = Guid.NewGuid();
        var batch = new StagingBatch
        {
            Id = batchId,
            BatchType = StagingBatchType.WebScrape,
            SupplierId = supplierId,
            StartedAt = DateTimeOffset.UtcNow,
            Status = ProcessingStatus.Started,
            FileContents = JsonSerializer.Serialize(cookies, JsonSerializerOptions.Web),
            FileHash = ComputeFileHash(JsonSerializer.Serialize(cookies, JsonSerializerOptions.Web)),
            Notes = $"Scraped at {DateTimeOffset.UtcNow}"
        };
        
        Context.StagingBatches.Add(batch);
        await Context.SaveChangesAsync(cancellationToken);

        try
        {
            await ScrapeBatchAsync(batch, cancellationToken);
            batch.Status = ProcessingStatus.Completed;
            batch.CompletedAt = DateTimeOffset.UtcNow;
            await Context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to scrape orders");
            batch.Status = ProcessingStatus.Failed;
            batch.ErrorMessage = ex.Message;
            await Context.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    // Core scraping workflow
    public async Task ScrapeBatchAsync(StagingBatch batch, CancellationToken cancellationToken)
    {
        var cookies = JsonSerializer.Deserialize<CookieFile>(batch.FileContents ?? "{}", JsonSerializerOptions.Web) ?? new CookieFile();
        using var session = SessionFactory.CreateSession(cookies);

        // Fetch orders list
        var ordersListHtml = await session.FetchPageAsync(GetOrdersListUrl(), cancellationToken);
        var orderSummaries = ParseOrdersListAsync(ordersListHtml, cancellationToken);

        // Process each order
        await foreach (var orderSummary in orderSummaries)
        {
            await Task.Delay(Configuration.RequestDelay, cancellationToken);
            
            var orderUrl = GetOrderDetailUrl(orderSummary);
            var stagingOrder = new StagingPurchaseOrder
            {
                Id = Guid.NewGuid(),
                StagingBatchId = batch.Id,
                SupplierReference = orderUrl,
                OrderDate = DateTimeOffset.UtcNow,
                RawData = JsonSerializer.Serialize(orderSummary),
                IsImported = false,
                Status = ProcessingStatus.Started
            };
            
            Context.StagingPurchaseOrders.Add(stagingOrder);
            await Context.SaveChangesAsync(cancellationToken);

            try
            {
                var orderDetailsHtml = await session.FetchPageAsync(orderUrl, cancellationToken);
                var order = await ParseOrderDetailsAsync(orderDetailsHtml, orderSummary, cancellationToken);
                await UpdateStagingPurchaseOrderAsync(stagingOrder, order, cancellationToken);
                
                stagingOrder.Status = ProcessingStatus.Completed;
                await Context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to scrape order: {OrderUrl}", orderUrl ?? "unknown");
                stagingOrder.Status = ProcessingStatus.Failed;
                stagingOrder.ErrorMessage = ex.Message;
                await Context.SaveChangesAsync(cancellationToken);
            }
        }
    }

    protected string ReplaceUrlTemplateValues(string template, Dictionary<string, string> values)
    {
        var result = template;
        foreach (var kvp in values)
        {
            result = result.Replace($"{{{kvp.Key}}}", kvp.Value);
        }
        return result;
    }

    private static string ComputeFileHash(string? json)
    {
        ArgumentNullException.ThrowIfNull(json);
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(hashBytes);
    }
}
```

### `SheinWebScraper.cs` (New File)

Concrete implementation for Shein.com (key methods shown):

```csharp
public class SheinWebScraper(
    MyMarketManagerDbContext context,
    ILogger<SheinWebScraper> logger,
    IOptions<ScraperConfiguration> configuration,
    IWebScraperSessionFactory sessionFactory)
    : WebScraper(context, logger, configuration, sessionFactory)
{
    private const string OrdersListUrl = "https://shein.com/user/orders/list";
    private const string OrderDetailUrlTemplate = "https://shein.com/user/orders/detail/{orderNumber}";

    public override string GetOrdersListUrl() => OrdersListUrl;
    
    public override string GetOrderDetailUrl(WebScraperOrderSummary order) 
        => ReplaceUrlTemplateValues(OrderDetailUrlTemplate, order);

    // Parses Shein's order list page looking for gbRawData JSON
    public override async IAsyncEnumerable<WebScraperOrderSummary> ParseOrdersListAsync(
        string ordersListHtml, 
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Extract JSON from gbRawData variable
        var match = Regex.Match(ordersListHtml, @"var\s+gbRawData\s*=\s*(\{.*?\});", RegexOptions.Singleline);
        if (!match.Success)
        {
            Logger.LogWarning("Could not find gbRawData in orders list page");
            yield break;
        }

        var jsonText = match.Groups[1].Value;
        var doc = JsonDocument.Parse(jsonText);
        
        // Navigate to order list in JSON structure
        if (!doc.RootElement.TryGetProperty("data", out var data) ||
            !data.TryGetProperty("orders", out var orders))
        {
            yield break;
        }

        foreach (var orderElement in orders.EnumerateArray())
        {
            var orderSummary = new WebScraperOrderSummary
            {
                RawData = orderElement.GetRawText()
            };

            // Extract key fields
            if (orderElement.TryGetProperty("order_sn", out var orderSn))
                orderSummary["orderNumber"] = orderSn.GetString() ?? "";

            if (orderElement.TryGetProperty("order_id", out var orderId))
                orderSummary["orderId"] = orderId.GetString() ?? "";

            yield return orderSummary;
        }
    }

    // Parses individual order detail page
    public override async Task<WebScraperOrder> ParseOrderDetailsAsync(
        string orderDetailHtml, 
        WebScraperOrderSummary orderSummary, 
        CancellationToken cancellationToken)
    {
        var order = new WebScraperOrder(orderSummary);
        
        // Extract gbRawData from detail page
        var match = Regex.Match(orderDetailHtml, @"var\s+gbRawData\s*=\s*(\{.*?\});", RegexOptions.Singleline);
        if (!match.Success)
        {
            Logger.LogWarning("Could not find gbRawData in order detail page");
            return order;
        }

        var jsonText = match.Groups[1].Value;
        order.RawData = jsonText;
        
        var doc = JsonDocument.Parse(jsonText);
        
        // Extract order-level fields
        if (doc.RootElement.TryGetProperty("data", out var data))
        {
            ExtractOrderFields(order, data);
            
            // Extract line items
            if (data.TryGetProperty("productInfo", out var productInfo))
            {
                foreach (var itemElement in productInfo.EnumerateArray())
                {
                    var item = new WebScraperOrderItem(order)
                    {
                        RawData = itemElement.GetRawText()
                    };
                    
                    ExtractItemFields(item, itemElement);
                    order.OrderItems.Add(item);
                }
            }
        }

        return order;
    }

    // Updates staging entities with scraped data
    public override async Task UpdateStagingPurchaseOrderAsync(
        StagingPurchaseOrder stagingOrder, 
        WebScraperOrder order, 
        CancellationToken cancellationToken)
    {
        // Update order-level data
        stagingOrder.RawData = order.RawData ?? stagingOrder.RawData;
        
        if (order.TryGetValue("billno", out var billno))
            stagingOrder.SupplierReference = billno;
        
        if (order.TryGetValue("addTime", out var addTime) && 
            long.TryParse(addTime, out var timestamp))
        {
            stagingOrder.OrderDate = DateTimeOffset.FromUnixTimeSeconds(timestamp);
        }

        // Create line items
        foreach (var item in order.OrderItems)
        {
            var stagingItem = new StagingPurchaseOrderItem
            {
                Id = Guid.NewGuid(),
                StagingPurchaseOrderId = stagingOrder.Id,
                RawData = item.RawData ?? ""
            };

            if (item.TryGetValue("goods_id", out var goodsId))
                stagingItem.SupplierReference = goodsId;
            
            if (item.TryGetValue("goods_name", out var goodsName))
                stagingItem.ProductDescription = goodsName;
            
            if (item.TryGetValue("goods_qty", out var goodsQty) && 
                int.TryParse(goodsQty, out var qty))
            {
                stagingItem.Quantity = qty;
            }

            if (item.TryGetValue("unit_price", out var unitPrice) && 
                decimal.TryParse(unitPrice, out var price))
            {
                stagingItem.UnitCost = price;
            }

            Context.StagingPurchaseOrderItems.Add(stagingItem);
        }

        await Context.SaveChangesAsync(cancellationToken);
    }

    private void ExtractOrderFields(WebScraperOrder order, JsonElement data)
    {
        // Extracts fields like billno, addTime, totalPrice, etc.
        // Implementation details omitted for brevity
    }

    private void ExtractItemFields(WebScraperOrderItem item, JsonElement itemElement)
    {
        // Extracts fields like goods_id, goods_name, goods_qty, unit_price, etc.
        // Implementation details omitted for brevity
    }
}
```

---

## 3. Data Model Changes

### `ProcessingStatus.cs` (Enum Update)

Changed from 3 states to 5 states with clearer semantics:

**Before:**
```csharp
public enum ProcessingStatus
{
    Pending,    // Not yet started or awaiting action
    Partial,    // Partially completed or in progress
    Complete    // Fully completed
}
```

**After:**
```csharp
public enum ProcessingStatus
{
    Queued,     // Queued to start
    Started,    // Currently running
    Completed,  // Completed successfully
    Failed,     // Failed with an error
    Cancelled   // Cancelled before completion
}
```

### `StagingBatchType.cs` (New Enum)

New enum to distinguish batch sources:

```csharp
public enum StagingBatchType
{
    WebScrape = 0,   // Data was scraped from a website
    BlobUpload = 1   // Data was uploaded from a blob/file
}
```

### `StagingBatch.cs` (Entity Update)

Enhanced to support web scraping workflow:

**Key Changes:**
- Added `BatchType` property to distinguish web scrapes from file uploads
- Made `SupplierId` nullable (was required before)
- Renamed `UploadDate` to `StartedAt` 
- Added `CompletedAt` for tracking completion time
- Added `ErrorMessage` for failure diagnostics
- Added `FileContents` to store cookie JSON for web scrapes

**Before:**
```csharp
public class StagingBatch : EntityBase
{
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public DateTimeOffset UploadDate { get; set; }
    
    [Required]
    public string FileHash { get; set; } = string.Empty;
    public ProcessingStatus Status { get; set; }
    public string? Notes { get; set; }

    public ICollection<StagingPurchaseOrder> StagingPurchaseOrders { get; set; } = new List<StagingPurchaseOrder>();
    public ICollection<StagingSale> StagingSales { get; set; } = new List<StagingSale>();
}
```

**After:**
```csharp
public class StagingBatch : EntityBase
{
    public StagingBatchType BatchType { get; set; }
    
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    
    [Required]
    public string FileHash { get; set; } = string.Empty;
    public ProcessingStatus Status { get; set; }
    public string? Notes { get; set; }
    public string? ErrorMessage { get; set; }
    public string? FileContents { get; set; }

    public ICollection<StagingPurchaseOrder> StagingPurchaseOrders { get; set; } = new List<StagingPurchaseOrder>();
    public ICollection<StagingSale> StagingSales { get; set; } = new List<StagingSale>();
}
```

### `StagingPurchaseOrder.cs` (Entity Update)

Added status tracking for individual orders:

**Added Properties:**
```csharp
public ProcessingStatus Status { get; set; }
public string? ErrorMessage { get; set; }
```

---

## 4. SheinCollector MAUI App Changes

### `MyMarketManager.SheinCollector.csproj`

Added reference to Scrapers.Core:

```xml
<ItemGroup>
  <ProjectReference Include="..\MyMarketManager.Scrapers.Core\MyMarketManager.Scrapers.Core.csproj" />
</ItemGroup>
```

### `CookieService.cs`

**Key Changes:**
1. Removed inline `CookieData` class definition (now uses `MyMarketManager.Scrapers.Core.CookieData`)
2. Added `CreateCookieFileJson()` method to generate `CookieFile` JSON format

**New Method:**
```csharp
public string CreateCookieFileJson(List<CookieData> cookies)
{
    var cookieFile = new CookieFile
    {
        Domain = "shein.com",
        CapturedAt = DateTimeOffset.UtcNow,
        ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
        Cookies = cookies.ToDictionary(c => c.Name, c => c),
        Metadata = new Dictionary<string, string>
        {
            { "source", "SheinCollector MAUI App" },
            { "platform", DeviceInfo.Platform.ToString() },
            { "version", AppInfo.VersionString }
        }
    };

    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    return JsonSerializer.Serialize(cookieFile, options);
}
```

### `MainPage.xaml`

Added "Copy JSON" button:

```xml
<Button x:Name="CopyJsonButton"
        Text="Copy JSON"
        Clicked="OnCopyJsonClicked"
        BackgroundColor="#0078D4"
        TextColor="White"
        WidthRequest="120"
        IsEnabled="False" />
```

### `MainPage.xaml.cs`

**Key Changes:**
1. Added `_lastCollectedCookies` field to cache collected cookies
2. Added `OnCopyJsonClicked` event handler for the new button
3. Updated status messages to mention the Copy JSON button
4. Button enables after successful cookie collection

**New Event Handler:**
```csharp
private async void OnCopyJsonClicked(object sender, EventArgs e)
{
    try
    {
        if (_lastCollectedCookies == null || _lastCollectedCookies.Count == 0)
        {
            await DisplayAlertAsync("No Cookies", "No cookies have been collected yet. Click 'Done' first.", "OK");
            return;
        }

        var json = _cookieService.CreateCookieFileJson(_lastCollectedCookies);
        await Clipboard.SetTextAsync(json);

        StatusLabel.Text = "✓ Cookie JSON copied to clipboard!";
        StatusLabel.TextColor = Colors.Green;

        await DisplayAlertAsync("Copied", "CookieFile JSON has been copied to clipboard. You can now paste it in your API request.", "OK");
    }
    catch (Exception ex)
    {
        StatusLabel.Text = $"Error copying to clipboard: {ex.Message}";
        StatusLabel.TextColor = Colors.Red;
        await DisplayAlertAsync("Error", $"Failed to copy to clipboard: {ex.Message}", "OK");
    }
}
```

---

## 5. Configuration Changes

### `appsettings.json`

Added scraper configuration section:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ScraperConfiguration": {
    "UserAgent": "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36",
    "AdditionalHeaders": {
      "accept": "text/html",
      "accept-language": "en-US",
      "cache-control": "no-cache",
      "upgrade-insecure-requests": "1"
    },
    "RequestDelay": "00:00:02",
    "MaxConcurrentRequests": 1,
    "RequestTimeout": "00:00:30"
  }
}
```

---

## Architecture Summary

### Workflow

1. **Cookie Collection** (MAUI App):
   - User logs into Shein.com via WebView
   - App extracts browser cookies
   - User clicks "Copy JSON" to get `CookieFile` JSON
   - JSON is posted to backend API

2. **Scraping Orchestration** (Backend):
   - API receives `CookieFile` and supplier ID
   - Creates `StagingBatch` with `BatchType = WebScrape`
   - Stores cookies in `FileContents` field
   - Calls `SheinWebScraper.StartScrapingAsync()`

3. **Scraping Execution**:
   - Creates HTTP session with authentication cookies
   - Fetches orders list page
   - Parses order summaries from embedded JSON
   - For each order:
     - Creates `StagingPurchaseOrder` with `Status = Started`
     - Fetches order detail page
     - Parses order details and line items
     - Updates staging entities
     - Sets `Status = Completed` or `Failed`

4. **Data Flow**:
   - Raw JSON stored in `RawData` fields
   - Parsed data populates entity properties
   - Existing import/reconciliation workflow processes staging data

### Key Design Patterns

- **Template Method**: `WebScraper` base class defines workflow, subclasses implement parsing
- **Factory Pattern**: `IWebScraperSessionFactory` creates configured HTTP clients
- **Repository Pattern**: Direct DbContext usage in scrapers
- **Async Streams**: `IAsyncEnumerable<T>` for order list parsing

### Extensibility

To add a new supplier scraper:

1. Create new class inheriting from `WebScraper`
2. Implement 5 abstract methods:
   - `GetOrdersListUrl()`
   - `GetOrderDetailUrl()`
   - `ParseOrdersListAsync()`
   - `ParseOrderDetailsAsync()`
   - `UpdateStagingPurchaseOrderAsync()`
3. Register with DI container
4. No changes to infrastructure needed

---

## Summary Statistics

- **New Projects**: 2 (Scrapers.Core, Scrapers)
- **New Files**: 17 source files
- **Modified Files**: 6 source files
- **New Enums**: 1 (StagingBatchType)
- **Modified Enums**: 1 (ProcessingStatus)
- **New Interfaces**: 2 (IWebScraperSession, IWebScraperSessionFactory)
- **New Classes**: 11
- **Modified Classes**: 3 (StagingBatch, StagingPurchaseOrder, CookieService)
- **Lines Added**: ~1,000+ lines of production code
