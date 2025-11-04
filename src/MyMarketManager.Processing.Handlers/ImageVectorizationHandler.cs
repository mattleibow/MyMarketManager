using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MyMarketManager.Processing.Handlers;

/// <summary>
/// Base class for image vectorization handlers.
/// Provides common functionality for generating and storing vector embeddings for images.
/// Derived classes specify what images to fetch and how to store the embeddings.
/// </summary>
/// <typeparam name="TWorkItem">The type of work item containing the image to vectorize.</typeparam>
public abstract class ImageVectorizationHandler<TWorkItem> : IWorkItemHandler<TWorkItem> 
    where TWorkItem : IWorkItem
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly ILogger _logger;

    protected ImageVectorizationHandler(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        ILogger logger)
    {
        _embeddingGenerator = embeddingGenerator ?? throw new ArgumentNullException(nameof(embeddingGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Fetches the next batch of work items to process.
    /// Derived classes implement this to query their specific data source.
    /// </summary>
    public abstract Task<IReadOnlyCollection<TWorkItem>> FetchNextAsync(int maxItems, CancellationToken cancellationToken);

    /// <summary>
    /// Processes a single work item by generating and storing the vector embedding.
    /// </summary>
    public async Task ProcessAsync(TWorkItem workItem, CancellationToken cancellationToken)
    {
        var imageUrl = GetImageUrl(workItem);
        var imageId = workItem.Id;

        try
        {
            _logger.LogInformation("Vectorizing image {ImageId} - {Url}", imageId, imageUrl);

            // Generate vector embedding
            var result = await _embeddingGenerator.GenerateAsync([imageUrl], cancellationToken: cancellationToken);
            var embedding = result.FirstOrDefault();

            if (embedding != null)
            {
                // Store vector
                await StoreEmbeddingAsync(workItem, embedding.Vector.ToArray(), cancellationToken);

                _logger.LogInformation(
                    "Successfully vectorized image {ImageId}. Vector dimensions: {Dimensions}",
                    imageId, embedding.Vector.Length);
            }
            else
            {
                _logger.LogWarning("No embedding generated for image {ImageId}", imageId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to vectorize image {ImageId}", imageId);
            throw;
        }
    }

    /// <summary>
    /// Gets the image URL from the work item.
    /// </summary>
    protected abstract string GetImageUrl(TWorkItem workItem);

    /// <summary>
    /// Stores the generated embedding.
    /// Derived classes implement this to save the embedding to their specific storage.
    /// </summary>
    protected abstract Task StoreEmbeddingAsync(TWorkItem workItem, float[] embedding, CancellationToken cancellationToken);
}
