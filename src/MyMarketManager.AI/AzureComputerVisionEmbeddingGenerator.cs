using Microsoft.Extensions.AI;
using System.Text.Json;

namespace MyMarketManager.AI;

/// <summary>
/// Base class for Azure Computer Vision embedding generators.
/// Handles common HTTP client operations and response parsing.
/// </summary>
public abstract class AzureComputerVisionEmbeddingGenerator(
    string embeddingType,
    string apiEndpoint,
    IHttpClientFactory httpClientFactory,
    string httpClientName,
    string modelVersion = "2023-04-15")
    : IEmbeddingGenerator<string, Embedding<float>>
{
    protected abstract object CreateRequestPayload(string value);

    public EmbeddingGeneratorMetadata Metadata => new($"AzureComputerVision{embeddingType}Embedding", null, modelVersion);

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var embeddings = new List<Embedding<float>>();

        foreach (var value in values)
        {
            using var httpClient = httpClientFactory.CreateClient(httpClientName);
            
            var request = new HttpRequestMessage(HttpMethod.Post, 
                $"computervision/{apiEndpoint}?api-version=2024-02-01&model-version={modelVersion}");
            request.Content = new StringContent(
                JsonSerializer.Serialize(CreateRequestPayload(value)),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<VectorResponse>(content);

            if (result?.Vector != null)
            {
                embeddings.Add(new Embedding<float>(result.Vector));
            }
        }

        return [.. embeddings];
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceType == typeof(EmbeddingGeneratorMetadata) ? Metadata : null;
    }

    public void Dispose()
    {
        // No unmanaged resources to dispose
    }

    protected class VectorResponse
    {
        public string? ModelVersion { get; set; }
        public float[]? Vector { get; set; }
    }
}
