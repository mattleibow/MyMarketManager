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
    protected ILogger<TScraper> ScraperLogger { get; } = Substitute.For<ILogger<TScraper>>();

    protected IOptions<ScraperConfiguration> ScraperConfig { get; } = CreateConfiguration();

    protected IWebScraperSessionFactory CreateMockSessionFactory(Dictionary<string, string?> mockResponses)
    {
        var session = Substitute.For<IWebScraperSession>();
        session.FetchPageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var url = callInfo.Arg<string>();
                if (mockResponses.TryGetValue(url, out var response) && response is not null)
                {
                    return Task.FromResult(response);
                }
                throw new InvalidOperationException($"No mock response configured for URL: {url}");
            });

        var factory = Substitute.For<IWebScraperSessionFactory>();
        factory.CreateSession(Arg.Any<CookieFile>())
            .Returns(session);
        return factory;
    }

    protected static IOptions<ScraperConfiguration> CreateConfiguration()
    {
        var config = ScraperConfiguration.Defaults;
        return Options.Create(config);
    }

    protected static string FixturesRootPath =>
        Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Fixtures");

    protected static bool FixtureFileExists(string fixturePath) =>
        File.Exists(Path.Combine(FixturesRootPath, fixturePath));

    protected static string LoadFixture(string fileName) =>
        File.ReadAllText(Path.Combine(FixturesRootPath, fileName));

    protected static string LoadHtmlFixture(string fileName) =>
        LoadFixture(Path.Combine("Html", fileName));
}
