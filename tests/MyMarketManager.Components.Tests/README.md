# MyMarketManager.Components.Tests

Unit tests for Blazor components using GraphQL client instead of direct database access.

## Overview

This test project contains unit tests for Blazor components that use the GraphQL client (`IMyMarketManagerClient`) for data operations. These components are designed to be reusable in:
- Blazor WASM applications
- MAUI mobile applications  
- Any .NET application that can host Blazor components

## Technology Stack

- **xUnit 3.0** - Test framework
- **bUnit 1.36** - Blazor component testing library
- **NSubstitute 5.3** - Mocking framework for dependencies
- **FluentAssertions 7.0** - Fluent assertion library

## Components Under Test

### ProductsGraphQL.razor
Product listing component with:
- Product search/filtering
- Delete confirmation modal
- Loading states
- Error handling
- Navigation to add/edit forms

### ProductFormGraphQL.razor
Product add/edit form with:
- Create and update modes
- Form validation
- Quality rating selection
- Stock management
- Quality guide sidebar

## Test Strategy

### Mocking the GraphQL Client

All tests mock the `IMyMarketManagerClient` interface using NSubstitute:

```csharp
var mockClient = Substitute.For<IMyMarketManagerClient>();
var mockQuery = Substitute.For<IGetProductsQuery>();
mockClient.GetProducts.Returns(mockQuery);
```

### Component Rendering

Tests use bUnit's `RenderComponent` to render components in isolation:

```csharp
var cut = RenderComponent<ProductsGraphQL>();
```

### Assertions

Tests use FluentAssertions for readable assertions:

```csharp
cut.Markup.Should().Contain("Products");
cut.Find("h1").Should().NotBeNull();
```

## Running Tests

From the repository root:

```bash
# Run all component tests
dotnet test tests/MyMarketManager.Components.Tests

# Run with verbose output
dotnet test tests/MyMarketManager.Components.Tests --verbosity normal

# Run in Release configuration
dotnet test tests/MyMarketManager.Components.Tests --configuration Release
```

## Test Coverage

Current tests cover:
- Component initialization
- Loading states
- Form field rendering
- Component modes (add vs edit)

## Adding New Tests

When adding new component tests:

1. **Create the component** in `Components/` directory
2. **Use GraphQL client only** - no direct DbContext dependencies
3. **Write unit tests** that mock `IMyMarketManagerClient`
4. **Test rendering** - verify UI elements are present
5. **Test interactions** - verify user actions work correctly
6. **Test error cases** - verify error messages display properly

Example test structure:

```csharp
[Fact]
public void ComponentName_Behavior_ExpectedResult()
{
    // Arrange - setup mocks
    var mockClient = Substitute.For<IMyMarketManagerClient>();
    Services.AddSingleton(mockClient);
    JSInterop.Mode = JSRuntimeMode.Loose;
    
    // Act - render component
    var cut = RenderComponent<YourComponent>();
    
    // Assert - verify behavior
    cut.Find("selector").Should().NotBeNull();
}
```

## Best Practices

### 1. Keep Tests Simple
- Test one behavior per test
- Use descriptive test names: `Component_Behavior_ExpectedResult`
- Avoid complex mocking setups

### 2. Mock Only What's Needed
- Don't mock everything - only mock dependencies
- Use `JSInterop.Mode = JSRuntimeMode.Loose` for JS interop

### 3. Use Helpers
- Create helper methods for common mock setups
- Reuse mock configurations across tests

### 4. Test User Interactions
- Test button clicks, form submissions, navigation
- Verify component state changes
- Check that GraphQL client methods are called correctly

## Dependencies

The test project depends on:
- `MyMarketManager.GraphQL.Client` - For GraphQL client interfaces and types

The test project does NOT depend on:
- `MyMarketManager.Data` - No database dependencies
- `MyMarketManager.WebApp` - No server-side dependencies

This ensures components can run in any .NET environment (WASM, MAUI, etc.).

## Troubleshooting

### Tests Fail with "Object reference not set"
Ensure you're mocking all required properties on the GraphQL client interfaces.

### Component Doesn't Render
Check that:
- `Services.AddSingleton(mockClient)` is called
- `JSInterop.Mode = JSRuntimeMode.Loose` is set
- All required dependencies are registered

### NavigationManager Errors
Mock the NavigationManager if the component uses navigation:

```csharp
var mockNavManager = Substitute.For<NavigationManager>();
Services.AddSingleton(mockNavManager);
```

## Future Enhancements

Potential additions:
- Integration tests with real GraphQL server
- Snapshot testing for UI regression
- Accessibility testing with axe-core
- Visual regression testing
- More complex user interaction scenarios
