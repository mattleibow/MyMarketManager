using System.Net;
using System.Text;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Xunit;

namespace MyMarketManager.Integration.Tests;

/// <summary>
/// Integration tests for the GraphQL endpoint using Aspire hosting
/// </summary>
public class GraphQLEndpointTests : IAsyncLifetime
{
    private DistributedApplication? _app;
    private HttpClient? _httpClient;

    public async ValueTask InitializeAsync()
    {
        // Set environment to Testing so SQLite is used
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        
        // Create the Aspire app with Testing environment to use SQLite
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.MyMarketManager_AppHost>();

        // Build and start the app
        _app = await appHost.BuildAsync();
        await _app.StartAsync();

        // Get the WebApp resource and create an HTTP client
        _httpClient = _app.CreateHttpClient("webapp");
    }

    public async ValueTask DisposeAsync()
    {
        _httpClient?.Dispose();
        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    [Fact]
    public async Task GraphQLEndpoint_IsAccessible()
    {
        // Arrange
        var query = new
        {
            query = "{ __schema { queryType { name } } }"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(query),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _httpClient!.PostAsync("/graphql", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync();
        Assert.Contains("Query", result);
    }

    [Fact]
    public async Task GraphQLEndpoint_ReturnsSchema()
    {
        // Arrange
        var introspectionQuery = new
        {
            query = @"
                query IntrospectionQuery {
                    __schema {
                        queryType { name }
                        mutationType { name }
                        types {
                            name
                            kind
                        }
                    }
                }"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(introspectionQuery),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _httpClient!.PostAsync("/graphql", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync();
        
        // Verify schema contains our types
        Assert.Contains("Product", result);
        Assert.Contains("Query", result);
        Assert.Contains("Mutation", result);
    }

    [Fact]
    public async Task GraphQLEndpoint_CanQueryProducts()
    {
        // Arrange
        var query = new
        {
            query = @"
                query {
                    products {
                        id
                        name
                    }
                }"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(query),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _httpClient!.PostAsync("/graphql", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync();
        
        // Should return valid JSON with data field
        Assert.Contains("\"data\"", result);
        Assert.Contains("\"products\"", result);
    }

    [Fact]
    public async Task GraphQLEndpoint_CanCreateProduct()
    {
        // Arrange
        var mutation = new
        {
            query = @"
                mutation {
                    createProduct(input: {
                        name: ""Test Product""
                        quality: GOOD
                        stockOnHand: 10
                    }) {
                        id
                        name
                        quality
                        stockOnHand
                    }
                }"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(mutation),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _httpClient!.PostAsync("/graphql", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync();
        
        // Should return the created product
        Assert.Contains("\"data\"", result);
        Assert.Contains("Test Product", result);
        Assert.Contains("GOOD", result);
    }

    [Fact]
    public async Task GraphQLEndpoint_ReturnsErrorForInvalidQuery()
    {
        // Arrange
        var query = new
        {
            query = "{ invalidField }"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(query),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _httpClient!.PostAsync("/graphql", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // GraphQL returns 200 even for errors
        var result = await response.Content.ReadAsStringAsync();
        
        // Should contain error information
        Assert.Contains("\"errors\"", result);
    }
}
