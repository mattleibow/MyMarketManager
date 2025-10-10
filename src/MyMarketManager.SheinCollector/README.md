# MyMarketManager.SheinCollector

A .NET MAUI mobile application for collecting cookies from the Shein website and making authenticated API requests.

## Overview

This application provides a WebView interface to login to Shein, collect authentication cookies, and then use those cookies to fetch order data from the Shein API.

## Features

- **WebView Login**: Displays the Shein login page in a native WebView
- **Cookie Collection**: Captures all `*.shein.com` cookies after user logs in
- **JSON Export**: Saves collected cookies to a JSON file for reuse
- **API Request**: Makes authenticated HTTP request to the orders endpoint
- **Response Validation**: Checks for `gbRawData` in the response to verify success

## How It Works

1. User opens the app and sees the Shein login page in a WebView
2. User logs in with their Shein credentials
3. User clicks the "Done - Collect Cookies" button
4. App collects all Shein cookies from the WebView
5. App saves cookies to `shein_cookies.json` in the app's data directory
6. App makes an HTTP request to `https://shein.com/user/orders/list` with:
   - All collected cookies
   - Required headers (accept, user-agent, etc.)
7. App displays success if `gbRawData` is found in the response

## Platform Support

- Android (API 21+)
- iOS (11.0+)
- macOS Catalyst (13.1+)
- Windows (10.0.17763.0+)

## Project Structure

- `MainPage.xaml/.cs` - Main UI with WebView and controls
- `CookieService.cs` - Cookie collection and HTTP request logic
- `MauiProgram.cs` - App initialization and dependency injection
- `Platforms/` - Platform-specific implementations for cookie access

## Building and Running

### Prerequisites

- .NET 10 SDK
- MAUI workload: `dotnet workload install maui`
- Platform-specific SDKs (Android SDK, Xcode for iOS/Mac, Windows SDK)

### Build

```bash
# Restore packages
dotnet restore

# Build for Android
dotnet build -f net10.0-android

# Build for iOS
dotnet build -f net10.0-ios

# Build for Windows
dotnet build -f net10.0-windows10.0.19041.0
```

### Run

```bash
# Run on Android
dotnet run -f net10.0-android

# Run on iOS
dotnet run -f net10.0-ios

# Run on Windows
dotnet run -f net10.0-windows10.0.19041.0
```

## Cookie Storage Location

Cookies are saved to the application's data directory:
- Android: `/data/data/com.mymarketmanager.sheincollector/files/shein_cookies.json`
- iOS: `~/Library/Application Support/shein_cookies.json`
- Windows: `%LOCALAPPDATA%\MyMarketManager.SheinCollector\shein_cookies.json`

## HTTP Request Details

The app makes a GET request to `https://shein.com/user/orders/list` with the following headers:

```
accept: text/html
accept-language: en-US
cache-control: no-cache
upgrade-insecure-requests: 1
user-agent: Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36 Edg/141.0.0.0
```

All collected cookies are included in the request.

## Success Criteria

The app considers the request successful when the response body contains the string `gbRawData`, which indicates that the authenticated order data has been returned.

## Security Notes

- This app is for demonstration purposes
- Cookies contain sensitive authentication data
- The `shein_cookies.json` file should be protected
- Do not share cookie files or commit them to version control
- Cookies may expire and require re-authentication

## Implementation Notes

### Platform-Specific Cookie Access

Each platform has its own way of accessing WebView cookies:

- **Android**: Uses `Android.Webkit.CookieManager`
- **iOS/macOS**: Uses `WebKit.WKWebsiteDataStore.HttpCookieStore`
- **Windows**: Uses `Microsoft.Web.WebView2` (simplified implementation)

The `CookieService` class contains platform-specific code using preprocessor directives.

## Dependencies

- Microsoft.Maui.Controls
- Microsoft.Maui.Controls.Compatibility
- Microsoft.Extensions.Logging.Debug (debug builds only)

## License

Same as the parent MyMarketManager project.
