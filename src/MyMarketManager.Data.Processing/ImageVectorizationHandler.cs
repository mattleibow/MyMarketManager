using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyMarketManager.Data;

namespace MyMarketManager.Data.Processing;

/// <summary>
/// Handler that fetches product photos without vector embeddings and generates embeddings.
/// This can be instantiated multiple times with different configurations for different sources
/// (e.g., product photos, delivery photos, etc.).
/// </summary>
public class ImageVectorizationHandler : IWorkItemHandler<ImageVectorizationWorkItem>
{
    private readonly MyMarketManagerDbContext _context;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly ILogger<ImageVectorizationHandler> _logger;
    private readonly string _name;
    private readonly int _maxItems;

    public ImageVectorizationHandler(
        MyMarketManagerDbContext context,
        [FromKeyedServices("image")] IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        ILogger<ImageVectorizationHandler> logger,
        string name = "ImageVectorization",
        int maxItemsPerCycle = 10)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _embeddingGenerator = embeddingGenerator ?? throw new ArgumentNullException(nameof(embeddingGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _name = name;
        _maxItems = maxItemsPerCycle;
    }

    public string Name => _name;

    public int MaxItemsPerCycle => _maxItems;

    public async Task<IReadOnlyCollection<ImageVectorizationWorkItem>> FetchWorkItemsAsync(
        int maxItems, 
        CancellationToken cancellationToken)
    {
        // Fetch photos without vector embeddings
        var pendingPhotos = await _context.ProductPhotos
            .Where(p => p.VectorEmbedding == null)
            .Take(maxItems)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Found {Count} product photos pending vectorization", pendingPhotos.Count);

        return pendingPhotos
            .Select(p => new ImageVectorizationWorkItem(p))
            .ToList();
    }

    public async Task ProcessAsync(ImageVectorizationWorkItem workItem, CancellationToken cancellationToken)
    {
        var photo = workItem.Photo;

        try
        {
            _logger.LogInformation("Vectorizing image {PhotoId} - {Url}", photo.Id, photo.Url);

            // Generate vector embedding
            var result = await _embeddingGenerator.GenerateAsync([photo.Url], cancellationToken: cancellationToken);
            var embedding = result.FirstOrDefault();

            if (embedding != null)
            {
                // Store vector
                photo.VectorEmbedding = embedding.Vector.ToArray();
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Successfully vectorized image {PhotoId}. Vector dimensions: {Dimensions}",
                    photo.Id, photo.VectorEmbedding.Length);
            }
            else
            {
                _logger.LogWarning("No embedding generated for image {PhotoId}", photo.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to vectorize image {PhotoId}", photo.Id);
            throw;
        }
    }
}
