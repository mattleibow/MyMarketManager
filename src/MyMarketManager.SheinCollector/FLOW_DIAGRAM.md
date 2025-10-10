# Shein Cookie Collector - Application Flow

## Visual Flow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    App Launches                              │
│                         │                                     │
│                         ▼                                     │
│  ┌──────────────────────────────────────────────────┐       │
│  │           MainPage with WebView                  │       │
│  │  ┌──────────────────────────────────────────┐   │       │
│  │  │  WebView shows:                           │   │       │
│  │  │  https://shein.com/user/auth/login       │   │       │
│  │  │                                           │   │       │
│  │  │  [User logs in with credentials]          │   │       │
│  │  └──────────────────────────────────────────┘   │       │
│  │                                                   │       │
│  │  [Done - Collect Cookies]  [Reload]             │       │
│  └──────────────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────────────┘
                         │
                         │ User clicks "Done"
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              CookieService.GetCookiesFromWebView()           │
│                         │                                     │
│                         ├─── Platform-specific code          │
│                         │                                     │
│  ┌──────────────┬──────┴──────┬──────────────┐             │
│  │   Android    │     iOS     │   Windows     │             │
│  │  CookieManager│ HttpCookie │   WebView2    │             │
│  └──────┬───────┴──────┬──────┴──────┬────────┘             │
│         │              │             │                       │
│         └──────────────┴─────────────┘                       │
│                         │                                     │
│                         ▼                                     │
│         List<CookieData> (all *.shein.com cookies)          │
└─────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│            CookieService.SaveCookiesToFile()                 │
│                         │                                     │
│         Serialize to JSON with indentation                   │
│                         │                                     │
│                         ▼                                     │
│  FileSystem.AppDataDirectory/shein_cookies.json             │
│                                                               │
│  [                                                           │
│    {                                                         │
│      "Name": "session_id",                                   │
│      "Value": "abc123...",                                   │
│      "Domain": ".shein.com",                                 │
│      "Path": "/",                                            │
│      "Secure": true,                                         │
│      "HttpOnly": true                                        │
│    },                                                        │
│    ...                                                       │
│  ]                                                           │
└─────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│         CookieService.FetchOrdersWithCookies()               │
│                         │                                     │
│  1. Create HttpClientHandler with CookieContainer            │
│  2. Add all cookies to container                             │
│  3. Add required headers:                                    │
│     • accept: text/html                                      │
│     • accept-language: en-US                                 │
│     • cache-control: no-cache                                │
│     • upgrade-insecure-requests: 1                           │
│     • user-agent: Mozilla/5.0...                             │
│  4. GET https://shein.com/user/orders/list                   │
│                         │                                     │
│                         ▼                                     │
│              HTTP Response (HTML/JSON)                        │
└─────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                  Validate Response                            │
│                         │                                     │
│         Does response contain "gbRawData"?                   │
│                         │                                     │
│              ┌──────────┴──────────┐                         │
│              │                     │                         │
│            YES                    NO                         │
│              │                     │                         │
│              ▼                     ▼                         │
│      ✓ Success!            ⚠ Warning!                       │
│      Green status          Orange status                     │
└─────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                  Display Results                              │
│                                                               │
│  Status: "✓ Success! Found 'gbRawData' in response."        │
│                                                               │
│  Result:                                                      │
│  Response length: 12345 characters                           │
│  First 500 chars:                                            │
│  <!DOCTYPE html>                                             │
│  <html>                                                      │
│  ...                                                         │
│  gbRawData = {...}                                           │
│  ...                                                         │
└─────────────────────────────────────────────────────────────┘
```

## Key Components

### 1. MainPage (MainPage.xaml / MainPage.xaml.cs)
- **WebView**: Displays the Shein login page
- **Done Button**: Triggers cookie collection and API request
- **Reload Button**: Refreshes the WebView
- **Status Label**: Shows current operation status
- **Result Label**: Displays response information

### 2. CookieService (CookieService.cs)
- **GetCookiesFromWebView()**: Platform-specific cookie extraction
  - Android: Uses `Android.Webkit.CookieManager`
  - iOS/macOS: Uses `WebKit.WKWebsiteDataStore.HttpCookieStore`
  - Windows: Uses `Microsoft.Web.WebView2` (simplified)
- **SaveCookiesToFile()**: Serializes cookies to JSON
- **FetchOrdersWithCookies()**: Makes authenticated HTTP request

### 3. CookieData (CookieService.cs)
- Simple POCO for cookie data
- Properties: Name, Value, Domain, Path, Secure, HttpOnly

## Data Flow

1. **User Input** → WebView (login credentials)
2. **WebView** → Browser stores cookies
3. **User Action** → Click "Done" button
4. **Platform API** → Extract cookies from WebView
5. **CookieService** → Convert to CookieData objects
6. **File System** → Save as JSON
7. **HTTP Client** → Send request with cookies and headers
8. **Shein API** → Return HTML response
9. **Validation** → Check for "gbRawData" string
10. **UI** → Display results to user

## Platform-Specific Implementation

### Android
```csharp
var cookieManager = Android.Webkit.CookieManager.Instance;
var cookieString = cookieManager.GetCookie("https://shein.com");
// Parse cookie string into CookieData objects
```

### iOS/macOS
```csharp
var cookieStore = WebKit.WKWebsiteDataStore.DefaultDataStore.HttpCookieStore;
var allCookies = await cookieStore.GetAllCookiesAsync();
// Filter for *.shein.com cookies
```

### Windows
```csharp
// Simplified implementation
// Full WebView2 cookie access requires additional setup
```

## Success Criteria

The application considers the operation successful when:

1. ✅ Cookies are successfully extracted from WebView
2. ✅ Cookie JSON file is created
3. ✅ HTTP request returns 200 OK
4. ✅ Response body contains the string "gbRawData"

## Error Handling

- Exceptions are caught and displayed in the UI
- Status label turns red on error
- Result label shows full exception details
- "Done" button is re-enabled after operation completes

## Security Considerations

- Cookies are stored in app's protected data directory
- File permissions restrict access to the app only
- Cookies may expire and require re-authentication
- No cloud storage or transmission of cookies
- JSON file is not encrypted (consider encryption for production)

## Future Enhancements

Possible improvements:
- [ ] Cookie encryption
- [ ] Automatic cookie refresh
- [ ] Multiple account support
- [ ] Export/import cookie functionality
- [ ] Background sync
- [ ] Push notifications for order updates
- [ ] Integration with MyMarketManager main app
