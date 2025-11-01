using Microsoft.Extensions.AI;
using System.Text.Json;

namespace MyMarketManager.AI;

/// <summary>
/// Azure Computer Vision embedding generator for product images.
/// Implements IEmbeddingGenerator for image URLs using the retrieval:vectorizeImage endpoint.
/// </summary>
public class AzureComputerVisionImageEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _httpClientName;
    private readonly string _modelVersion;

    public AzureComputerVisionImageEmbeddingGenerator(
        IHttpClientFactory httpClientFactory,
        string httpClientName,
        string modelVersion = "2023-04-15")
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _httpClientName = httpClientName ?? throw new ArgumentNullException(nameof(httpClientName));
        _modelVersion = modelVersion;
    }

    public EmbeddingGeneratorMetadata Metadata => new("AzureComputerVisionImageEmbedding", null, _modelVersion);

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var embeddings = new List<Embedding<float>>();

        foreach (var imageUrl in values)
        {
            var httpClient = _httpClientFactory.CreateClient(_httpClientName);
            
            var request = new HttpRequestMessage(HttpMethod.Post, 
                $"computervision/retrieval:vectorizeImage?api-version=2024-02-01&model-version={_modelVersion}");
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

        return new GeneratedEmbeddings<Embedding<float>>(embeddings);
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceType == typeof(EmbeddingGeneratorMetadata) ? Metadata : null;
    }

    public void Dispose()
    {
        // HttpClient instances are managed by the factory
    }

    private class VectorResponse
    {
        public string? ModelVersion { get; set; }
        public float[]? Vector { get; set; }
    }
}
