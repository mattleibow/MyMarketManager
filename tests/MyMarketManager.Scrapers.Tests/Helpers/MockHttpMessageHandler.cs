using System.Net;

namespace MyMarketManager.Scrapers.Tests.Helpers;

/// <summary>
/// Mock HTTP message handler that returns HTML fixtures instead of making real HTTP requests.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, string> _urlToFixtureMap;

    public MockHttpMessageHandler(Dictionary<string, string> urlToFixtureMap)
    {
        _urlToFixtureMap = urlToFixtureMap ?? throw new ArgumentNullException(nameof(urlToFixtureMap));
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri?.ToString() ?? string.Empty;

        // Try to find exact match
        if (_urlToFixtureMap.TryGetValue(url, out var fixtureName))
        {
            var html = HtmlFixtureLoader.Load(fixtureName);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(html, System.Text.Encoding.UTF8, "text/html")
            };
            return Task.FromResult(response);
        }

        // Try to find partial match (for URLs with query params)
        foreach (var kvp in _urlToFixtureMap)
        {
            if (url.Contains(kvp.Key))
            {
                var html = HtmlFixtureLoader.Load(kvp.Value);
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(html, System.Text.Encoding.UTF8, "text/html")
                };
                return Task.FromResult(response);
            }
        }

        // No match found
        var notFoundResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent($"No fixture found for URL: {url}")
        };
        return Task.FromResult(notFoundResponse);
    }
}
