using Microsoft.Extensions.AI;
using System.Text.Json;

namespace MyMarketManager.AI;

/// <summary>
/// Azure Computer Vision embedding generator for product images.
/// Implements IEmbeddingGenerator for image URLs using the retrieval:vectorizeImage endpoint.
/// </summary>
public class ImageEmbeddingGenerator(
    IHttpClientFactory httpClientFactory,
    string httpClientName,
    string modelVersion = "2023-04-15")
    : IEmbeddingGenerator<string, Embedding<float>>
{
    public EmbeddingGeneratorMetadata Metadata => new("AzureComputerVisionImageEmbedding", null, modelVersion);

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var embeddings = new List<Embedding<float>>();

        foreach (var imageUrl in values)
        {
            using var httpClient = httpClientFactory.CreateClient(httpClientName);
            
            var request = new HttpRequestMessage(HttpMethod.Post, 
                $"computervision/retrieval:vectorizeImage?api-version=2024-02-01&model-version={modelVersion}");
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { url = imageUrl }),
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
