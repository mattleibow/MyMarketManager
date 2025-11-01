using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace MyMarketManager.AI;

/// <summary>
/// Extension methods for registering Azure Computer Vision embedding generators.
/// </summary>
public static class AzureComputerVisionEmbeddingExtensions
{
    private const string HttpClientName = "AzureComputerVisionEmbedding";

    /// <summary>
    /// Adds Azure Computer Vision embedding generators as keyed services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="endpoint">Azure Computer Vision endpoint URL.</param>
    /// <param name="apiKey">Azure Computer Vision API key.</param>
    /// <param name="modelVersion">Model version to use (default: 2023-04-15 for multilingual support).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAzureComputerVisionEmbeddings(
        this IServiceCollection services,
        string endpoint,
        string apiKey,
        string modelVersion = "2023-04-15")
    {
        // Register a single named HttpClient with proper configuration
        services.AddHttpClient(HttpClientName, client =>
        {
            client.BaseAddress = new Uri(endpoint);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
            client.Timeout = TimeSpan.FromSeconds(100);
        })
        .SetHandlerLifetime(TimeSpan.FromMinutes(5));

        // Register image embedding generator as keyed service with key "image"
        services.AddKeyedSingleton<IEmbeddingGenerator<string, Embedding<float>>>("image", (sp, key) =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            return new AzureComputerVisionImageEmbeddingGenerator(
                httpClientFactory,
                HttpClientName,
                modelVersion);
        });

        // Register text embedding generator as keyed service with key "text"
        services.AddKeyedSingleton<IEmbeddingGenerator<string, Embedding<float>>>("text", (sp, key) =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            return new AzureComputerVisionTextEmbeddingGenerator(
                httpClientFactory,
                HttpClientName,
                modelVersion);
        });

        return services;
    }
}
