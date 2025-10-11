using HotChocolate.Execution;
using MyMarketManager.GraphQL.Client;
using StrawberryShake;
using System.Text.Json;
using SSOperationRequest = StrawberryShake.OperationRequest;
using SSExecutionStrategy = StrawberryShake.ExecutionStrategy;
using SSLocation = StrawberryShake.Location;
using SSIOperationResult = StrawberryShake.IOperationResult;
using HotChocolate;

namespace MyMarketManager.WebApp.Services;

/// <summary>
/// Server-side implementation of IMyMarketManagerClient that uses HotChocolate's IRequestExecutor
/// to execute GraphQL operations directly without HTTP overhead.
/// </summary>
public class ServerSideGraphQLClient : IMyMarketManagerClient
{
    private readonly IRequestExecutor _executor;

    public ServerSideGraphQLClient(IRequestExecutor executor)
    {
        _executor = executor;
    }

    public IGetProductsQuery GetProducts => new ServerSideGetProductsQuery(_executor);
    public IDeleteProductMutation DeleteProduct => new ServerSideDeleteProductMutation(_executor);
    public IUpdateProductMutation UpdateProduct => new ServerSideUpdateProductMutation(_executor);
    public IGetProductByIdQuery GetProductById => new ServerSideGetProductByIdQuery(_executor);
    public ICreateProductMutation CreateProduct => new ServerSideCreateProductMutation(_executor);
}

// Base classes for operations
internal abstract class ServerSideQueryBase<TResult> where TResult : class
{
    protected readonly IRequestExecutor Executor;

    protected ServerSideQueryBase(IRequestExecutor executor)
    {
        Executor = executor;
    }

    protected async Task<IOperationResult<TResult>> ExecuteInternalAsync(
        string operationName,
        string query,
        Dictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
    {
        // Build the GraphQL request using HotChocolate's request builder
        var requestBuilder = OperationRequestBuilder.New();
        requestBuilder.SetDocument(query);

        if (variables != null)
        {
            foreach (var (key, value) in variables)
            {
                requestBuilder.SetVariableValues(new Dictionary<string, object?> { { key, value } });
            }
        }

        var request = requestBuilder.Build();
        var result = await Executor.ExecuteAsync(request, cancellationToken);

        return MapResult<TResult>(result);
    }

    private static IOperationResult<TResult> MapResult<T>(IExecutionResult executionResult) where T : class
    {
        List<IClientError>? errors = null;
        
        // Try to extract errors - IExecutionResult may have an Errors property via pattern
        var errorsProperty = executionResult.GetType().GetProperty("Errors");
        if (errorsProperty != null)
        {
            var errorsValue = errorsProperty.GetValue(executionResult);
            if (errorsValue is IReadOnlyList<HotChocolate.IError> hotChocolateErrors && hotChocolateErrors.Count > 0)
            {
                errors = new List<IClientError>();
                foreach (var error in hotChocolateErrors)
                {
                    errors.Add(new ServerSideClientError(error));
                }
            }
        }

        T? data = null;
        
        // Serialize the result to JSON and deserialize using System.Text.Json
        var toJsonMethod = executionResult.GetType().GetMethod("ToJson", Type.EmptyTypes);
        if (toJsonMethod != null)
        {
            var json = toJsonMethod.Invoke(executionResult, null) as string;
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var jsonDoc = JsonDocument.Parse(json);
                    if (jsonDoc.RootElement.TryGetProperty("data", out var dataElement))
                    {
                        // Use System.Text.Json to deserialize
                        var dataJson = dataElement.GetRawText();
                        data = JsonSerializer.Deserialize<T>(dataJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    // Add deserialization error
                    var deserializationError = new ServerSideClientError(
                        $"Failed to deserialize response: {ex.Message}",
                        "DESERIALIZATION_ERROR");
                    errors = errors ?? new List<IClientError>();
                    errors.Add(deserializationError);
                }
            }
        }

        return (IOperationResult<TResult>)(object)new ServerSideOperationResult<T>(data, errors);
    }
}

// Query implementations
internal class ServerSideGetProductsQuery : ServerSideQueryBase<IGetProductsResult>, IGetProductsQuery
{
    public ServerSideGetProductsQuery(IRequestExecutor executor) : base(executor) { }

    public Task<IOperationResult<IGetProductsResult>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return ExecuteInternalAsync(
            "GetProducts",
            @"query GetProducts {
                products {
                    id
                    sku
                    name
                    description
                    quality
                    notes
                    stockOnHand
                }
            }",
            null,
            cancellationToken);
    }

    // Unsupported methods for server-side execution
    public IGetProductsQuery With(Action<SSOperationRequest> configure) => this;
    public IGetProductsQuery WithRequestUri(Uri requestUri) => this;
    public IGetProductsQuery WithHttpClient(HttpClient httpClient) => this;
    public IObservable<IOperationResult<IGetProductsResult>> Watch(SSExecutionStrategy? strategy = null) 
        => throw new NotSupportedException("Watch is not supported on server-side execution");
    public SSOperationRequest Create(IReadOnlyDictionary<string, object?>? variables = null) 
        => throw new NotSupportedException("Create is not supported on server-side execution");
    public Type ResultType => typeof(IGetProductsResult);
}

internal class ServerSideGetProductByIdQuery : ServerSideQueryBase<IGetProductByIdResult>, IGetProductByIdQuery
{
    public ServerSideGetProductByIdQuery(IRequestExecutor executor) : base(executor) { }

    public Task<IOperationResult<IGetProductByIdResult>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return ExecuteInternalAsync(
            "GetProductById",
            @"query GetProductById($id: UUID!) {
                productById(id: $id) {
                    id
                    sku
                    name
                    description
                    quality
                    notes
                    stockOnHand
                }
            }",
            new Dictionary<string, object?> { { "id", id } },
            cancellationToken);
    }

    public IGetProductByIdQuery With(Action<SSOperationRequest> configure) => this;
    public IGetProductByIdQuery WithRequestUri(Uri requestUri) => this;
    public IGetProductByIdQuery WithHttpClient(HttpClient httpClient) => this;
    public IObservable<IOperationResult<IGetProductByIdResult>> Watch(Guid id, SSExecutionStrategy? strategy = null) 
        => throw new NotSupportedException("Watch is not supported on server-side execution");
    public SSOperationRequest Create(IReadOnlyDictionary<string, object?>? variables = null) 
        => throw new NotSupportedException("Create is not supported on server-side execution");
    public Type ResultType => typeof(IGetProductByIdResult);
}

internal class ServerSideCreateProductMutation : ServerSideQueryBase<ICreateProductResult>, ICreateProductMutation
{
    public ServerSideCreateProductMutation(IRequestExecutor executor) : base(executor) { }

    public Task<IOperationResult<ICreateProductResult>> ExecuteAsync(CreateProductInput input, CancellationToken cancellationToken = default)
    {
        return ExecuteInternalAsync(
            "CreateProduct",
            @"mutation CreateProduct($input: CreateProductInput!) {
                createProduct(input: $input) {
                    id
                    sku
                    name
                    description
                    quality
                    notes
                    stockOnHand
                }
            }",
            new Dictionary<string, object?> { { "input", SerializeInput(input) } },
            cancellationToken);
    }

    private static Dictionary<string, object?> SerializeInput(CreateProductInput input)
    {
        return new Dictionary<string, object?>
        {
            { "sku", input.Sku },
            { "name", input.Name },
            { "description", input.Description },
            { "quality", input.Quality.ToString().ToUpperInvariant() },
            { "notes", input.Notes },
            { "stockOnHand", input.StockOnHand }
        };
    }

    public ICreateProductMutation With(Action<SSOperationRequest> configure) => this;
    public ICreateProductMutation WithRequestUri(Uri requestUri) => this;
    public ICreateProductMutation WithHttpClient(HttpClient httpClient) => this;
    public IObservable<IOperationResult<ICreateProductResult>> Watch(CreateProductInput input, SSExecutionStrategy? strategy = null) 
        => throw new NotSupportedException("Watch is not supported on server-side execution");
    public SSOperationRequest Create(IReadOnlyDictionary<string, object?>? variables = null) 
        => throw new NotSupportedException("Create is not supported on server-side execution");
    public Type ResultType => typeof(ICreateProductResult);
}

internal class ServerSideUpdateProductMutation : ServerSideQueryBase<IUpdateProductResult>, IUpdateProductMutation
{
    public ServerSideUpdateProductMutation(IRequestExecutor executor) : base(executor) { }

    public Task<IOperationResult<IUpdateProductResult>> ExecuteAsync(Guid id, UpdateProductInput input, CancellationToken cancellationToken = default)
    {
        return ExecuteInternalAsync(
            "UpdateProduct",
            @"mutation UpdateProduct($id: UUID!, $input: UpdateProductInput!) {
                updateProduct(id: $id, input: $input) {
                    id
                    sku
                    name
                    description
                    quality
                    notes
                    stockOnHand
                }
            }",
            new Dictionary<string, object?> 
            { 
                { "id", id },
                { "input", SerializeInput(input) }
            },
            cancellationToken);
    }

    private static Dictionary<string, object?> SerializeInput(UpdateProductInput input)
    {
        return new Dictionary<string, object?>
        {
            { "sku", input.Sku },
            { "name", input.Name },
            { "description", input.Description },
            { "quality", input.Quality.ToString().ToUpperInvariant() },
            { "notes", input.Notes },
            { "stockOnHand", input.StockOnHand }
        };
    }

    public IUpdateProductMutation With(Action<SSOperationRequest> configure) => this;
    public IUpdateProductMutation WithRequestUri(Uri requestUri) => this;
    public IUpdateProductMutation WithHttpClient(HttpClient httpClient) => this;
    public IObservable<IOperationResult<IUpdateProductResult>> Watch(Guid id, UpdateProductInput input, SSExecutionStrategy? strategy = null) 
        => throw new NotSupportedException("Watch is not supported on server-side execution");
    public SSOperationRequest Create(IReadOnlyDictionary<string, object?>? variables = null) 
        => throw new NotSupportedException("Create is not supported on server-side execution");
    public Type ResultType => typeof(IUpdateProductResult);
}

internal class ServerSideDeleteProductMutation : ServerSideQueryBase<IDeleteProductResult>, IDeleteProductMutation
{
    public ServerSideDeleteProductMutation(IRequestExecutor executor) : base(executor) { }

    public Task<IOperationResult<IDeleteProductResult>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return ExecuteInternalAsync(
            "DeleteProduct",
            @"mutation DeleteProduct($id: UUID!) {
                deleteProduct(id: $id)
            }",
            new Dictionary<string, object?> { { "id", id } },
            cancellationToken);
    }

    public IDeleteProductMutation With(Action<SSOperationRequest> configure) => this;
    public IDeleteProductMutation WithRequestUri(Uri requestUri) => this;
    public IDeleteProductMutation WithHttpClient(HttpClient httpClient) => this;
    public IObservable<IOperationResult<IDeleteProductResult>> Watch(Guid id, SSExecutionStrategy? strategy = null) 
        => throw new NotSupportedException("Watch is not supported on server-side execution");
    public SSOperationRequest Create(IReadOnlyDictionary<string, object?>? variables = null) 
        => throw new NotSupportedException("Create is not supported on server-side execution");
    public Type ResultType => typeof(IDeleteProductResult);
}

// Simple error and result implementations
internal class ServerSideClientError : IClientError
{
    public ServerSideClientError(HotChocolate.IError error)
    {
        Message = error.Message;
        Code = error.Code;
        Path = error.Path?.ToList();
        Extensions = error.Extensions;
        Exception = error.Exception;
        Locations = error.Locations?.Select(l => new SSLocation(l.Line, l.Column)).ToList();
    }

    public ServerSideClientError(string message, string? code = null)
    {
        Message = message;
        Code = code;
    }

    public string Message { get; }
    public string? Code { get; }
    public IReadOnlyList<object?>? Path { get; }
    public IReadOnlyDictionary<string, object?>? Extensions { get; }
    public Exception? Exception { get; }
    public IReadOnlyList<SSLocation>? Locations { get; }
}

internal class ServerSideOperationResult<T> : IOperationResult<T> where T : class
{
    public ServerSideOperationResult(T? data, IReadOnlyList<IClientError>? errors)
    {
        Data = data;
        Errors = errors ?? Array.Empty<IClientError>();
        Extensions = new Dictionary<string, object?>();
        ContextData = new Dictionary<string, object?>();
    }

    public T? Data { get; }
    object? SSIOperationResult.Data => Data;
    public Type DataType => typeof(T);
    public IReadOnlyList<IClientError> Errors { get; }
    public IReadOnlyDictionary<string, object?> Extensions { get; }
    public IOperationResultDataFactory<T>? DataFactory => null;
    object? SSIOperationResult.DataFactory => DataFactory;
    public IOperationResultDataInfo? DataInfo => null;
    public IReadOnlyDictionary<string, object?> ContextData { get; }
    
    public IOperationResult<T> WithData(T data, IOperationResultDataInfo dataInfo)
    {
        return new ServerSideOperationResult<T>(data, Errors);
    }
}
