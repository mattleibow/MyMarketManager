using HotChocolate;
using HotChocolate.Execution;
using MartinCostello.Logging.XUnit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyMarketManager.GraphQL.Server;
using MyMarketManager.Tests.Shared;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyMarketManager.GraphQL.Server.Tests;

/// <summary>
/// Base class for GraphQL tests that provides a configured GraphQL executor
/// </summary>
public abstract class GraphQLTestBase : SqliteTestBase
{
    protected IRequestExecutor Executor { get; set; } = null!;
    private readonly ITestOutputHelper _outputHelper;

    protected GraphQLTestBase(ITestOutputHelper outputHelper, bool createSchema = true)
        : base(outputHelper, createSchema)
    {
        _outputHelper = outputHelper;
    }

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        // Build the GraphQL schema with the test database context
        Executor = await new ServiceCollection()
            .AddSingleton(Context)
            .AddLogging(builder => builder.AddXUnit(_outputHelper))
            .AddMyMarketManagerGraphQLServer()
            .BuildRequestExecutorAsync();
    }

    /// <summary>
    /// Execute a GraphQL query and return the result
    /// </summary>
    protected async Task<IExecutionResult> ExecuteRequestAsync(string query, Dictionary<string, object?>? variables = null)
    {
        var request = OperationRequestBuilder.New()
            .SetDocument(query)
            .SetVariableValues(variables)
            .Build();

        return await Executor.ExecuteAsync(request, Cancel);
    }

    /// <summary>
    /// Execute a GraphQL query and get the data as T
    /// </summary>
    protected async Task<T> ExecuteQueryAsync<T>(string query, Dictionary<string, object?>? variables = null)
    {
        var result = await ExecuteRequestAsync(query, variables);
        
        // Assert no errors
        if (result is IOperationResult operationResult && operationResult.Errors?.Count > 0)
        {
            var errors = string.Join(", ", operationResult.Errors.Select(e => e.Message));
            throw new Exception($"GraphQL errors: {errors}");
        }

        // Get the data
        var json = result.ToJson();
        var document = JsonDocument.Parse(json);
        
        if (!document.RootElement.TryGetProperty("data", out var dataElement))
        {
            throw new Exception($"No data in GraphQL response: {json}");
        }
        
        var options = new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
        
        return JsonSerializer.Deserialize<T>(dataElement.GetRawText(), options)!;
    }
}
