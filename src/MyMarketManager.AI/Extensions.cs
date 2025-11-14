using Azure.AI.Inference;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace MyMarketManager.AI;

/// <summary>
/// Extension methods for registering Azure AI Foundry embedding generators.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds Azure AI Foundry embedding generators using a connection string with role-based authentication.
    /// Registers both image and text embedding generators using appropriate client types.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">Azure AI Foundry connection string (format: Endpoint=https://...;EndpointAIInference=https://...;Deployment=model-name).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAzureAIFoundryEmbeddings(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        var (endpoint, modelName) = ParseConnectionString(connectionString);
        var credential = new DefaultAzureCredential();
        var clientOptions = new AzureAIInferenceClientOptions();
        var tokenPolicy = new BearerTokenAuthenticationPolicy(credential, ["https://cognitiveservices.azure.com/.default"]);

        clientOptions.AddPolicy(tokenPolicy, HttpPipelinePosition.PerRetry);

        // Register text embedding generator using EmbeddingsClient
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            var client = new EmbeddingsClient(new Uri(endpoint), credential, clientOptions);
            return client.AsIEmbeddingGenerator(modelName);
        });

        // Register image embedding generator using ImageEmbeddingsClient
        services.AddSingleton<IEmbeddingGenerator<DataContent, Embedding<float>>>(sp =>
        {
            var client = new ImageEmbeddingsClient(new Uri(endpoint), credential, clientOptions);
            return client.AsIEmbeddingGenerator(modelName);
        });

        return services;
    }

    /// <summary>
    /// Adds a no-op embedding generator when Azure AI is not configured.
    /// This allows the app to start without Azure AI credentials, but operations will throw
    /// an exception if attempted. Useful for development and CI environments.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNoOpEmbeddingGenerator(this IServiceCollection services)
    {
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(new NoOpEmbeddingGenerator<string>());
        services.AddSingleton<IEmbeddingGenerator<DataContent, Embedding<float>>>(new NoOpEmbeddingGenerator<DataContent>());
        return services;
    }

    private static (string endpoint, string modelName) ParseConnectionString(string connectionString)
    {
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        string? endpointAIInference = null;
        string? deployment = null;

        foreach (var part in parts)
        {
            var keyValue = part.Split('=', 2);
            if (keyValue.Length != 2) continue;

            var key = keyValue[0].Trim();
            var value = keyValue[1].Trim();

            if (key.Equals("EndpointAIInference", StringComparison.OrdinalIgnoreCase))
                endpointAIInference = value;
            else if (key.Equals("Deployment", StringComparison.OrdinalIgnoreCase))
                deployment = value;
        }

        if (string.IsNullOrEmpty(endpointAIInference))
            throw new ArgumentException("Connection string must contain an 'EndpointAIInference' value.", nameof(connectionString));
        if (string.IsNullOrEmpty(deployment))
            throw new ArgumentException("Connection string must contain a 'Deployment' value.", nameof(connectionString));

        return (endpointAIInference, deployment);
    }
}
