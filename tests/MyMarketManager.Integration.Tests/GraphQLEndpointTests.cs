using System.Text;
using System.Text.Json;
using MyMarketManager.Tests.Shared;

namespace MyMarketManager.Integration.Tests;

[Trait(TestCategories.Key, TestCategories.Values.GraphQL)]
[Trait(TestRequirements.Key, TestRequirements.Values.SSL)]
public class GraphQLEndpointTests(ITestOutputHelper outputHelper) : WebAppTestsBase(outputHelper)
{
    [Fact]
    public async Task Endpoint_IsAccessible()
    {
        // Arrange
        var query = new
        {
            query =
                """
                {
                    __schema {
                        queryType {
                            name
                        }
                    }
                }
                """
        };

        // Act
        var response = await PostAsync(query);

        // Assert
        var result = await response.Content.ReadAsStringAsync(Cancel);
        Assert.Contains("Query", result);
    }

    [Fact]
    public async Task Endpoint_ReturnsSchema()
    {
        // Arrange
        var introspectionQuery = new
        {
            query =
                """
                query IntrospectionQuery {
                    __schema {
                        queryType { name }
                        mutationType { name }
                        types {
                            name
                            kind
                        }
                    }
                }
                """
        };

        // Act
        var response = await PostAsync(introspectionQuery);

        // Assert
        var result = await response.Content.ReadAsStringAsync(Cancel);

        // Verify schema contains root Query and Mutation types
        Assert.Contains("Query", result);
        Assert.Contains("Mutation", result);
    }

    [Fact]
    public async Task Endpoint_CanQueryProducts()
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

        // Act
        var response = await PostAsync(query);

        // Assert
        var result = await response.Content.ReadAsStringAsync(Cancel);

        // Should return valid JSON with data field
        Assert.Contains("\"data\"", result);
        Assert.Contains("\"products\"", result);
    }

    [Fact]
    public async Task Endpoint_CanCreateProduct()
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

        // Act
        var response = await PostAsync(mutation);

        // Assert
        var result = await response.Content.ReadAsStringAsync(Cancel);

        // Should return the created product
        Assert.Contains("\"data\"", result);
        Assert.Contains("Test Product", result);
        Assert.Contains("GOOD", result);
    }

    [Fact]
    public async Task Endpoint_ReturnsErrorForInvalidQuery()
    {
        // Arrange
        var query = new
        {
            query = "{ invalidField }"
        };

        // Act
        var response = await PostAsync(query, HttpStatusCode.BadRequest);

        // Assert
        var result = await response.Content.ReadAsStringAsync(Cancel);

        // Should contain error information
        Assert.Contains("\"errors\"", result);
    }

    protected async Task<HttpResponseMessage> PostAsync<TValue>(TValue query, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(query),
            Encoding.UTF8,
            "application/json");

        var response = await WebAppHttpClient.PostAsync("/graphql", content, Cancel);

        Assert.Equal(expectedStatusCode, response.StatusCode);

        return response;
    }
}
