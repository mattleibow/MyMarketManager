# Shein Cookie Collector - Build and Test Instructions

## Prerequisites

Since this is a .NET MAUI application, it requires the MAUI workload to be installed:

```bash
dotnet workload install maui
```

**Note:** MAUI workloads are not available in all CI/CD environments. This application must be built on a system with:
- Visual Studio 2022 with MAUI workload, OR
- .NET 10 SDK with MAUI workload installed

## Building the Application

### Android

```bash
cd src/MyMarketManager.SheinCollector
dotnet restore
dotnet build -f net10.0-android -c Release
```

### iOS

```bash
cd src/MyMarketManager.SheinCollector
dotnet restore
dotnet build -f net10.0-ios -c Release
```

Requires macOS with Xcode installed.

### Windows

```bash
cd src/MyMarketManager.SheinCollector
dotnet restore
dotnet build -f net10.0-windows10.0.19041.0 -c Release
```

## Running the Application

### Android Emulator

```bash
dotnet run -f net10.0-android
```

### iOS Simulator

```bash
dotnet run -f net10.0-ios
```

### Windows

```bash
dotnet run -f net10.0-windows10.0.19041.0
```

## Testing the Application

### Manual Test Steps

1. **Launch the app**
   - The app should display a WebView with the Shein login page

2. **Login to Shein**
   - Enter your Shein credentials
   - Complete the login process
   - You should be redirected to your account page

3. **Collect Cookies**
   - Click the "Done - Collect Cookies" button
   - The app will display "Collecting cookies..."
   - Status will update with the path to the saved JSON file

4. **Verify Cookie File**
   - Check that `shein_cookies.json` was created in the app's data directory
   - The file should contain JSON array of cookies with properties:
     - `Name`
     - `Value`
     - `Domain`
     - `Path`
     - `Secure`
     - `HttpOnly`

5. **Verify API Request**
   - The app automatically sends a request to `https://shein.com/user/orders/list`
   - Status should show: "✓ Success! Found 'gbRawData' in response." (green)
   - Result label shows response length and first 500 characters

6. **Expected Success Criteria**
   - Response contains the string `gbRawData`
   - Response length > 0
   - No errors displayed

### Troubleshooting

**Login Issues:**
- Ensure you have a valid Shein account
- Check internet connectivity
- Some regions may have different Shein domains

**Cookie Collection Issues:**
- Make sure you're fully logged in before clicking "Done"
- Try the "Reload" button to refresh the WebView

**API Request Issues:**
- Cookies may expire - try logging in again
- Network errors may occur - check connectivity
- Some regions may require different endpoints

## Development Environment

This application was built for CI/CD systems that do not support MAUI workloads. The project structure is complete and follows MAUI best practices:

- ✅ Project file with multi-targeting
- ✅ Platform-specific code
- ✅ Resources (icons, splash, styles)
- ✅ Dependency injection
- ✅ Service layer for cookie management
- ✅ Cross-platform WebView implementation

## Known Limitations

1. **MAUI Workload Required**: Cannot be built in environments without MAUI workload
2. **Platform-Specific**: Cookie access varies by platform
3. **Windows Implementation**: Simplified - full WebView2 cookie access requires additional setup
4. **No Unit Tests**: MAUI apps are typically tested manually or with UI testing frameworks

## Integration with MyMarketManager

This is a standalone utility app separate from the main MyMarketManager solution. It does not:
- Share the database
- Use the GraphQL API
- Require the Aspire AppHost

It can be deployed independently as a mobile app for collecting Shein authentication data.

## File Locations

When running the app, cookies are saved to:

- **Android**: `/data/data/com.mymarketmanager.sheincollector/files/shein_cookies.json`
- **iOS**: `~/Library/Application Support/shein_cookies.json`
- **macOS**: `~/Library/Application Support/shein_cookies.json`
- **Windows**: `%LOCALAPPDATA%\MyMarketManager.SheinCollector\shein_cookies.json`

To access the file on Android:
```bash
adb shell
run-as com.mymarketmanager.sheincollector
cat files/shein_cookies.json
```

To access on iOS (requires jailbreak or simulator):
```bash
# On simulator
cat ~/Library/Developer/CoreSimulator/Devices/[DEVICE-ID]/data/Containers/Data/Application/[APP-ID]/Library/Application Support/shein_cookies.json
```

## Security Considerations

- Cookie files contain sensitive authentication tokens
- Do not commit cookie files to version control
- Do not share cookie files
- Cookies expire and require re-authentication
- The app stores cookies in the app's protected data directory
