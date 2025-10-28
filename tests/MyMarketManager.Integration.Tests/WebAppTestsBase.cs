using System.Text;
using System.Text.Json;

namespace MyMarketManager.Integration.Tests;

public class WebAppTestsBase(ITestOutputHelper outputHelper) : AppHostTestsBase(outputHelper)
{
    protected HttpClient WebAppHttpClient { get; private set; } = null!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        WebAppHttpClient = App.CreateHttpClient("webapp");
    }

    public override ValueTask DisposeAsync()
    {
        WebAppHttpClient?.Dispose();

        return base.DisposeAsync();
    }

    /// <summary>
    /// Posts a GraphQL query or mutation to the /graphql endpoint
    /// </summary>
    protected async Task<HttpResponseMessage> PostGraphQLAsync<TValue>(TValue query, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
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
