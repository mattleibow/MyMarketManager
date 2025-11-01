using Microsoft.Extensions.AI;
using System.Text.Json;

namespace MyMarketManager.AI;

/// <summary>
/// Azure Computer Vision embedding generator for text queries.
/// Implements IEmbeddingGenerator for semantic text search using the retrieval:vectorizeText endpoint.
/// </summary>
public class TextEmbeddingGenerator(
    IHttpClientFactory httpClientFactory,
    string httpClientName,
    string modelVersion = "2023-04-15")
    : IEmbeddingGenerator<string, Embedding<float>>
{
    public EmbeddingGeneratorMetadata Metadata => new("AzureComputerVisionTextEmbedding", null, modelVersion);

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var embeddings = new List<Embedding<float>>();

        foreach (var text in values)
        {
            using var httpClient = httpClientFactory.CreateClient(httpClientName);
            
            var request = new HttpRequestMessage(HttpMethod.Post, 
                $"computervision/retrieval:vectorizeText?api-version=2024-02-01&model-version={modelVersion}");
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { text }),
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

    private class VectorResponse
    {
        public string? ModelVersion { get; set; }
        public float[]? Vector { get; set; }
    }
}
