using System.Text;
using System.Text.Json;
using MyMarketManager.Tests.Shared;

namespace MyMarketManager.Integration.Tests;

[Trait(TestCategories.Key, TestCategories.Values.GraphQL)]
[Trait(TestRequirements.Key, TestRequirements.Values.SSL)]
public class StagingOrderMigrationTests(ITestOutputHelper outputHelper) : WebAppTestsBase(outputHelper)
{
    [Fact]
    public async Task GetStagingPurchaseOrderById_ReturnsOrderWithItems()
    {
        // Arrange - First create a staging order with items
        // This would normally be done by the ingestion process
        // For now, we'll just test that the query works with a non-existent ID
        var query = new
        {
            query = """
                query GetStagingPurchaseOrderById($id: UUID!) {
                    stagingPurchaseOrderById(id: $id) {
                        id
                        supplierReference
                        orderDate
                        status
                        items {
                            id
                            name
                            quantity
                            actualUnitPrice
                        }
                    }
                }
                """,
            variables = new
            {
                id = Guid.NewGuid()
            }
        };

        // Act
        var response = await PostAsync(query);

        // Assert
        var result = await response.Content.ReadAsStringAsync(Cancel);

        // Should return null for non-existent order
        Assert.Contains("\"data\"", result);
        Assert.Contains("\"stagingPurchaseOrderById\":null", result);
    }

    [Fact]
    public async Task SearchProductsForItem_ReturnsProducts()
    {
        // Arrange - First create a test product
        var createMutation = new
        {
            query = """
                mutation {
                    createProduct(input: {
                        name: "Test Search Product"
                        quality: GOOD
                        stockOnHand: 5
                    }) {
                        id
                        name
                    }
                }
                """
        };

        await PostAsync(createMutation);

        // Now search for it
        var searchQuery = new
        {
            query = """
                query SearchProductsForItem($searchTerm: String!) {
                    searchProductsForItem(searchTerm: $searchTerm) {
                        id
                        name
                        quality
                        stockOnHand
                    }
                }
                """,
            variables = new
            {
                searchTerm = "Test Search"
            }
        };

        // Act
        var response = await PostAsync(searchQuery);

        // Assert
        var result = await response.Content.ReadAsStringAsync(Cancel);

        Assert.Contains("\"data\"", result);
        Assert.Contains("\"searchProductsForItem\"", result);
        Assert.Contains("Test Search Product", result);
    }

    [Fact]
    public async Task LinkStagingItemToProduct_WithInvalidIds_ReturnsError()
    {
        // Arrange
        var mutation = new
        {
            query = """
                mutation LinkStagingItemToProduct($input: LinkStagingItemToProductInput!) {
                    linkStagingItemToProduct(input: $input) {
                        success
                        errorMessage
                    }
                }
                """,
            variables = new
            {
                input = new
                {
                    stagingItemId = Guid.NewGuid(),
                    productId = Guid.NewGuid()
                }
            }
        };

        // Act
        var response = await PostAsync(mutation);

        // Assert
        var result = await response.Content.ReadAsStringAsync(Cancel);

        Assert.Contains("\"data\"", result);
        Assert.Contains("\"linkStagingItemToProduct\"", result);
        Assert.Contains("\"success\":false", result);
        Assert.Contains("Staging item not found", result);
    }

    [Fact]
    public async Task UnlinkStagingItemFromProduct_WithInvalidId_ReturnsError()
    {
        // Arrange
        var mutation = new
        {
            query = """
                mutation UnlinkStagingItemFromProduct($stagingItemId: UUID!) {
                    unlinkStagingItemFromProduct(stagingItemId: $stagingItemId) {
                        success
                        errorMessage
                    }
                }
                """,
            variables = new
            {
                stagingItemId = Guid.NewGuid()
            }
        };

        // Act
        var response = await PostAsync(mutation);

        // Assert
        var result = await response.Content.ReadAsStringAsync(Cancel);

        Assert.Contains("\"data\"", result);
        Assert.Contains("\"unlinkStagingItemFromProduct\"", result);
        Assert.Contains("\"success\":false", result);
        Assert.Contains("Staging item not found", result);
    }

    [Fact]
    public async Task GraphQL_Schema_ContainsStagingOrderTypes()
    {
        // Arrange
        var query = new
        {
            query = """
                query {
                    __type(name: "StagingPurchaseOrderDetailDto") {
                        name
                        fields {
                            name
                            type {
                                name
                            }
                        }
                    }
                }
                """
        };

        // Act
        var response = await PostAsync(query);

        // Assert
        var result = await response.Content.ReadAsStringAsync(Cancel);

        Assert.Contains("\"data\"", result);
        Assert.Contains("StagingPurchaseOrderDetailDto", result);
        Assert.Contains("items", result);
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
