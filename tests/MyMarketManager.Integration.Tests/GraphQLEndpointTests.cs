using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace MyMarketManager.Integration.Tests;

/// <summary>
/// Integration tests for the GraphQL endpoint
/// </summary>
public class GraphQLEndpointTests : IClassFixture<GraphQLWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GraphQLEndpointTests(GraphQLWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
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
        var response = await _client.PostAsync("/graphql", content);

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
        var response = await _client.PostAsync("/graphql", content);

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
        var response = await _client.PostAsync("/graphql", content);

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
        var response = await _client.PostAsync("/graphql", content);

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
        var response = await _client.PostAsync("/graphql", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // GraphQL returns 200 even for errors
        var result = await response.Content.ReadAsStringAsync();
        
        // Should contain error information
        Assert.Contains("\"errors\"", result);
    }
}
