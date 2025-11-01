using MyMarketManager.Data.Enums;
using MyMarketManager.Data.Processing;
using MyMarketManager.GraphQL.Server;
using MyMarketManager.Tests.Shared;
using NSubstitute;

namespace MyMarketManager.GraphQL.Server.Tests;

[Trait(TestCategories.Key, TestCategories.Values.GraphQL)]
public class PurchaseOrderIngestionQueriesTests(ITestOutputHelper outputHelper) : SqliteTestBase(outputHelper, createSchema: true)
{
    private PurchaseOrderIngestionQueries Queries => new();

    [Fact]
    public void GetAvailableScrapers_ShouldReturnScrapersFromFactory()
    {
        // Arrange
        var factory = Substitute.For<IBatchProcessorFactory>();
        factory.GetAvailableProcessors(StagingBatchType.WebScrape)
            .Returns(new[] { "Shein", "Amazon", "Etsy" });

        // Act
        var result = Queries.GetAvailableScrapers(StagingBatchType.WebScrape, factory);

        // Assert
        Assert.Equal(3, result.Count());
        Assert.Contains("Shein", result);
        Assert.Contains("Amazon", result);
        Assert.Contains("Etsy", result);
    }

    [Fact]
    public void GetAvailableScrapers_WithNoScrapers_ShouldReturnEmpty()
    {
        // Arrange
        var factory = Substitute.For<IBatchProcessorFactory>();
        factory.GetAvailableProcessors(StagingBatchType.WebScrape)
            .Returns(Enumerable.Empty<string>());

        // Act
        var result = Queries.GetAvailableScrapers(StagingBatchType.WebScrape, factory);

        // Assert
        Assert.Empty(result);
    }
}
