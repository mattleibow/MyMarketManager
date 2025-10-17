using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMarketManager.Data.Tests;
using MyMarketManager.Scrapers.Core;
using MyMarketManager.Tests.Shared;
using NSubstitute;

namespace MyMarketManager.Scrapers.Tests;

[Trait(TestCategories.Key, TestCategories.Values.Database)]
public class WebScraperTests<TScraper>(ITestOutputHelper outputHelper) : SqliteTestBase(outputHelper)
    where TScraper : WebScraper
{
    protected ILogger<TScraper> ScraperLogger { get; } = CreateLogger();

    protected IOptions<ScraperConfiguration> ScraperConfig { get; } = CreateConfiguration();

    protected IWebScraperSessionFactory CreateMockSessionFactory(Dictionary<string, string?> mockResponses)
    {
        var factory = Substitute.For<IWebScraperSessionFactory>();
        factory.CreateSession(Arg.Any<CookieFile>())
            .Returns(_ => new MockWebScraperSession(mockResponses));
        return factory;
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

    /// <summary>
    /// Mock implementation of IWebScraperSession for testing without HTTP.
    /// </summary>
    class MockWebScraperSession(Dictionary<string, string?> mockResponses) : IWebScraperSession
    {
        private bool _disposed;

        public Task<string> FetchPageAsync(string url, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (mockResponses.TryGetValue(url, out var response) && response is not null)
            {
                return Task.FromResult(response);
            }

            throw new InvalidOperationException($"No mock response configured for URL: {url}");
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
