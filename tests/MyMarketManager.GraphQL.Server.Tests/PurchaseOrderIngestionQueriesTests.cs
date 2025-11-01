using HotChocolate.Execution;
using MyMarketManager.Data.Enums;
using MyMarketManager.Data.Processing;
using MyMarketManager.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MartinCostello.Logging.XUnit;
using NSubstitute;

namespace MyMarketManager.GraphQL.Server.Tests;

[Trait(TestCategories.Key, TestCategories.Values.GraphQL)]
public class PurchaseOrderIngestionQueriesTests(ITestOutputHelper outputHelper) : GraphQLTestBase(outputHelper, createSchema: true)
{
    private readonly ITestOutputHelper _output = outputHelper;

    public override async ValueTask InitializeAsync()
    {
        // Register a mock factory before initializing the base
        var factory = Substitute.For<IBatchProcessorFactory>();
        factory.GetAvailableProcessors(Arg.Any<StagingBatchType>())
            .Returns(new[] { "Shein", "Amazon", "Etsy" });

        // Initialize base first to get the Context
        await base.InitializeAsync();
        
        // Override the executor with one that has the factory
        Executor = await new ServiceCollection()
            .AddSingleton(Context)
            .AddSingleton(factory)
            .AddLogging(builder => builder.AddXUnit(_output))
            .AddMyMarketManagerGraphQLServer()
            .BuildRequestExecutorAsync();
    }

    [Fact]
    public async Task GetAvailableScrapers_ShouldReturnScrapersFromFactory()
    {
        // Act
        var result = await ExecuteQueryAsync<AvailableScrapersResponse>("""
            query {
                availableScrapers(batchType: WEB_SCRAPE)
            }
        """);

        // Assert
        Assert.NotNull(result.AvailableScrapers);
        Assert.Equal(3, result.AvailableScrapers.Count);
        Assert.Contains("Shein", result.AvailableScrapers);
        Assert.Contains("Amazon", result.AvailableScrapers);
        Assert.Contains("Etsy", result.AvailableScrapers);
    }

    [Fact]
    public async Task GetAvailableScrapers_WithNoScrapers_ShouldReturnEmpty()
    {
        // Override with an empty factory for this test
        var factory = Substitute.For<IBatchProcessorFactory>();
        factory.GetAvailableProcessors(Arg.Any<StagingBatchType>())
            .Returns(Enumerable.Empty<string>());

        Executor = await new ServiceCollection()
            .AddSingleton(Context)
            .AddSingleton(factory)
            .AddLogging(builder => builder.AddXUnit(_output))
            .AddMyMarketManagerGraphQLServer()
            .BuildRequestExecutorAsync();

        // Act
        var result = await ExecuteQueryAsync<AvailableScrapersResponse>("""
            query {
                availableScrapers(batchType: WEB_SCRAPE)
            }
        """);

        // Assert
        Assert.NotNull(result.AvailableScrapers);
        Assert.Empty(result.AvailableScrapers);
    }

    private record AvailableScrapersResponse(List<string> AvailableScrapers);
}

