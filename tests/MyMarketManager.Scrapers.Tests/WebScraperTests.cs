using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMarketManager.Data.Tests;
using MyMarketManager.Tests.Shared;
using NSubstitute;

namespace MyMarketManager.Scrapers.Tests;

[Trait(TestCategories.Key, TestCategories.Values.Database)]
public class WebScraperTests<TScraper>(ITestOutputHelper outputHelper) : SqliteTestBase(outputHelper)
    where TScraper : WebScraper
{
    protected ILogger<TScraper> ScraperLogger { get; } = CreateLogger();

    protected IOptions<ScraperConfiguration> ScraperConfig { get; } = CreateConfiguration();

    protected void MockResponses(WebScraper webScraper, Dictionary<string, string?>? customResponses = null)
    {
        webScraper.CreateHttpClient(Arg.Any<HttpClientHandler>())
            .Returns(x => new FixturesHttpClient(x.ArgAt<HttpClientHandler>(0), customResponses));
    }

    protected static ILogger<TScraper> CreateLogger()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        return loggerFactory.CreateLogger<TScraper>();
    }

    protected static IOptions<ScraperConfiguration> CreateConfiguration()
    {
        var config = new ScraperConfiguration();
        return Options.Create(config);
    }

    protected string LoadHtmlFixture(string fileName)
    {
        // Get the directory where the test assembly is located
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation) ?? throw new InvalidOperationException("Could not determine assembly directory");
        
        // Construct path to fixtures directory
        var fixturePath = Path.Combine(assemblyDir, "Fixtures", "Html", fileName);

        if (!File.Exists(fixturePath))
        {
            throw new FileNotFoundException($"HTML fixture '{fileName}' not found at: {fixturePath}");
        }

        return File.ReadAllText(fixturePath);
    }

    class FixturesHttpClient(HttpMessageHandler handler, Dictionary<string, string?>? customResponses) : HttpClient(handler)
    {
        public override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri?.AbsoluteUri;

            if (customResponses?.TryGetValue(uri ?? string.Empty, out var customResponse) != true)
                throw new NotSupportedException($"Unknown request URL: {uri}");

            var response = customResponse is null
                ? new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)
                : new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(customResponse) };

            return Task.FromResult(response);
        }
    }
}
