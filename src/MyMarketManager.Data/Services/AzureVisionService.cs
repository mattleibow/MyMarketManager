using System.Text.Json;
using Azure;
using Azure.AI.Vision.ImageAnalysis;
using Microsoft.Extensions.Logging;

namespace MyMarketManager.Data.Services;

/// <summary>
/// Implementation of Azure AI Vision service for image analysis and vectorization.
/// Uses Azure AI Vision 4.0 APIs for multimodal embeddings and image analysis.
/// </summary>
public class AzureVisionService : IAzureVisionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AzureVisionService> _logger;
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly ImageAnalysisClient? _analysisClient;

    public AzureVisionService(
        HttpClient httpClient,
        ILogger<AzureVisionService> logger,
        string endpoint,
        string apiKey)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));

        // Only create analysis client if credentials are provided
        if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(apiKey))
        {
            _analysisClient = new ImageAnalysisClient(
                new Uri(endpoint),
                new AzureKeyCredential(apiKey));
        }
    }

    /// <summary>
    /// Analyzes an image to generate captions and tags using Azure AI Vision 4.0.
    /// </summary>
    public async Task<ImageAnalysisResult> AnalyzeImageAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        if (_analysisClient == null)
        {
            throw new InvalidOperationException("Azure Vision client is not configured. Please provide endpoint and API key.");
        }

        try
        {
            _logger.LogInformation("Analyzing image: {ImageUrl}", imageUrl);

            var result = await _analysisClient.AnalyzeAsync(
                new Uri(imageUrl),
                VisualFeatures.Caption | VisualFeatures.Tags,
                new ImageAnalysisOptions { GenderNeutralCaption = true },
                cancellationToken);

            var analysisResult = new ImageAnalysisResult
            {
                Caption = result.Value.Caption.Text,
                CaptionConfidence = result.Value.Caption.Confidence,
                Tags = result.Value.Tags.Values
                    .Select(t => new ImageTag { Name = t.Name, Confidence = t.Confidence })
                    .ToList()
            };

            _logger.LogInformation("Analysis complete. Caption: {Caption}, Tags: {TagCount}",
                analysisResult.Caption, analysisResult.Tags.Count);

            return analysisResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing image: {ImageUrl}", imageUrl);
            throw;
        }
    }

    /// <summary>
    /// Vectorizes an image into a 1024-dimensional embedding using Azure AI Vision multimodal embeddings.
    /// </summary>
    public async Task<float[]> VectorizeImageAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Vectorizing image: {ImageUrl}", imageUrl);

            // Use the latest model version (2023-04-15) which supports multilingual text search
            var requestUrl = $"{_endpoint}/computervision/retrieval:vectorizeImage?api-version=2024-02-01&model-version=2023-04-15";

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { url = imageUrl }),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var vectorResponse = JsonSerializer.Deserialize<VectorResponse>(content);

            if (vectorResponse == null || vectorResponse.Vector == null)
            {
                throw new InvalidOperationException("Failed to get vector from response");
            }

            _logger.LogInformation("Image vectorized successfully. Vector dimensions: {Dimensions}",
                vectorResponse.Vector.Length);

            return vectorResponse.Vector;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error vectorizing image: {ImageUrl}", imageUrl);
            throw;
        }
    }

    /// <summary>
    /// Vectorizes text into a 1024-dimensional embedding using Azure AI Vision multimodal embeddings.
    /// </summary>
    public async Task<float[]> VectorizeTextAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Vectorizing text: {Text}", text);

            // Use the latest model version (2023-04-15) which supports multilingual text search
            var requestUrl = $"{_endpoint}/computervision/retrieval:vectorizeText?api-version=2024-02-01&model-version=2023-04-15";

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { text }),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var vectorResponse = JsonSerializer.Deserialize<VectorResponse>(content);

            if (vectorResponse == null || vectorResponse.Vector == null)
            {
                throw new InvalidOperationException("Failed to get vector from response");
            }

            _logger.LogInformation("Text vectorized successfully. Vector dimensions: {Dimensions}",
                vectorResponse.Vector.Length);

            return vectorResponse.Vector;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error vectorizing text: {Text}", text);
            throw;
        }
    }

    /// <summary>
    /// Calculates cosine similarity between two vectors.
    /// Returns a value between 0 and 1, where 1 means identical vectors.
    /// </summary>
    public float CalculateCosineSimilarity(float[] vector1, float[] vector2)
    {
        if (vector1.Length != vector2.Length)
        {
            throw new ArgumentException("Vectors must have the same length");
        }

        float dotProduct = 0;
        float magnitude1 = 0;
        float magnitude2 = 0;

        for (int i = 0; i < vector1.Length; i++)
        {
            dotProduct += vector1[i] * vector2[i];
            magnitude1 += vector1[i] * vector1[i];
            magnitude2 += vector2[i] * vector2[i];
        }

        magnitude1 = (float)Math.Sqrt(magnitude1);
        magnitude2 = (float)Math.Sqrt(magnitude2);

        if (magnitude1 == 0 || magnitude2 == 0)
        {
            return 0;
        }

        return dotProduct / (magnitude1 * magnitude2);
    }

    /// <summary>
    /// Response from Azure AI Vision vectorization API.
    /// </summary>
    private class VectorResponse
    {
        public string? ModelVersion { get; set; }
        public float[]? Vector { get; set; }
    }
}
