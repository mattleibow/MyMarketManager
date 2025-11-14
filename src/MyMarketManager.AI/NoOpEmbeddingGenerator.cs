using Microsoft.Extensions.AI;

namespace MyMarketManager.AI;

/// <summary>
/// No-op embedding generator that throws when Azure AI services are not configured.
/// </summary>
public class NoOpEmbeddingGenerator<TInput> : IEmbeddingGenerator<TInput, Embedding<float>>
{
    public EmbeddingGeneratorMetadata Metadata => new("NoOp", null, null);

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<TInput> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "Azure AI embedding services are not configured. " +
            "Please provide Azure AI Foundry connection string in configuration.");
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceType == typeof(EmbeddingGeneratorMetadata) ? Metadata : null;
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}
