using Microsoft.Extensions.AI;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace MyMarketManager.Processing.Handlers;

/// <summary>
/// Base class for image vectorization handlers.
/// Provides common functionality for generating and storing vector embeddings for images.
/// Derived classes specify what images to fetch and how to store the embeddings.
/// </summary>
/// <typeparam name="TWorkItem">The type of work item - must inherit from ImageVectorizationWorkItem.</typeparam>
public abstract class ImageVectorizationHandler<TWorkItem> : IWorkItemHandler<TWorkItem> 
    where TWorkItem : ImageVectorizationWorkItem
{
    private readonly IEmbeddingGenerator<DataContent, Embedding<float>> _embeddingGenerator;
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;

    protected ImageVectorizationHandler(
        IEmbeddingGenerator<DataContent, Embedding<float>> embeddingGenerator,
        IHttpClientFactory httpClientFactory,
        ILogger logger)
    {
        _embeddingGenerator = embeddingGenerator ?? throw new ArgumentNullException(nameof(embeddingGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClientFactory?.CreateClient() ?? throw new ArgumentNullException(nameof(httpClientFactory));
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
        try
        {
            _logger.LogInformation("Vectorizing image {ImageId} - {Url}", workItem.Id, workItem.ImageUrl);

            // Download the image
            var imageBytes = await _httpClient.GetByteArrayAsync(workItem.ImageUrl, cancellationToken);
            
            // Create DataContent from byte array with MIME type from entity
            var imageContent = new DataContent(imageBytes, workItem.MimeType);

            // Generate vector embedding
            var result = await _embeddingGenerator.GenerateAsync([imageContent], cancellationToken: cancellationToken);
            var embedding = result.FirstOrDefault();

            if (embedding != null)
            {
                // Store vector
                await StoreEmbeddingAsync(workItem, embedding.Vector.ToArray(), cancellationToken);

                _logger.LogInformation(
                    "Successfully vectorized image {ImageId}. Vector dimensions: {Dimensions}",
                    workItem.Id, embedding.Vector.Length);
            }
            else
            {
                _logger.LogWarning("No embedding generated for image {ImageId}", workItem.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to vectorize image {ImageId}", workItem.Id);
            throw;
        }
    }

    /// <summary>
    /// Stores the generated embedding.
    /// Derived classes implement this to save the embedding to their specific storage.
    /// The work item's entity is already tracked in this scope's DbContext.
    /// </summary>
    protected abstract Task StoreEmbeddingAsync(TWorkItem workItem, float[] embedding, CancellationToken cancellationToken);
}

