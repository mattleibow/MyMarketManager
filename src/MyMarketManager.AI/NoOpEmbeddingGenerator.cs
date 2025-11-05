using Microsoft.Extensions.AI;

namespace MyMarketManager.AI;

/// <summary>
/// A no-op embedding generator that throws when used.
/// This is registered when Azure AI services are not configured,
/// allowing the app to start but preventing actual embedding operations.
/// </summary>
public class NoOpEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    public EmbeddingGeneratorMetadata Metadata => new("NoOp", null, "none");

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "Azure AI embedding services are not configured. " +
            "Please provide Azure AI Foundry endpoint and API key in configuration.");
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceType == typeof(EmbeddingGeneratorMetadata) ? Metadata : null;
    }

    public void Dispose()
    {
        // No resources to dispose
    }
}
