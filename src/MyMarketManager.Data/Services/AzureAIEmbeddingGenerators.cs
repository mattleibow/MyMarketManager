using Microsoft.Extensions.AI;
using System.Text.Json;

namespace MyMarketManager.Data.Services;

/// <summary>
/// Azure AI Foundry embedding generator for product images.
/// Implements IEmbeddingGenerator for image URLs stored in blob storage.
/// </summary>
public class AzureAIImageEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _apiKey;

    public AzureAIImageEmbeddingGenerator(
        HttpClient httpClient,
        string endpoint,
        string apiKey)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
    }

    public EmbeddingGeneratorMetadata Metadata => new("AzureAIImageEmbeddingGenerator", new Uri(_endpoint), "Cohere-embed-v3-english");

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var embeddings = new List<Embedding<float>>();

        foreach (var imageUrl in values)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_endpoint}/models/images/embeddings?api-version=2024-05-01-preview");
            request.Headers.Add("api-key", _apiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    model = "Cohere-embed-v3-english",
                    input = new[] { new { image = imageUrl } },
                    input_type = "document"
                }),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<EmbeddingResponse>(content);

            if (result?.Data != null && result.Data.Length > 0)
            {
                embeddings.Add(new Embedding<float>(result.Data[0].Embedding));
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
        // HttpClient is managed externally
    }

    private class EmbeddingResponse
    {
        public EmbeddingData[]? Data { get; set; }
    }

    private class EmbeddingData
    {
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}

/// <summary>
/// Azure AI Foundry embedding generator for text queries.
/// Implements IEmbeddingGenerator for semantic text search.
/// </summary>
public class AzureAITextEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _apiKey;

    public AzureAITextEmbeddingGenerator(
        HttpClient httpClient,
        string endpoint,
        string apiKey)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
    }

    public EmbeddingGeneratorMetadata Metadata => new("AzureAITextEmbeddingGenerator", new Uri(_endpoint), "Cohere-embed-v3-english");

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var embeddings = new List<Embedding<float>>();

        foreach (var text in values)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_endpoint}/models/embeddings?api-version=2024-05-01-preview");
            request.Headers.Add("api-key", _apiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    model = "Cohere-embed-v3-english",
                    input = new[] { text },
                    input_type = "query"
                }),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<EmbeddingResponse>(content);

            if (result?.Data != null && result.Data.Length > 0)
            {
                embeddings.Add(new Embedding<float>(result.Data[0].Embedding));
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
        // HttpClient is managed externally
    }

    private class EmbeddingResponse
    {
        public EmbeddingData[]? Data { get; set; }
    }

    private class EmbeddingData
    {
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}
