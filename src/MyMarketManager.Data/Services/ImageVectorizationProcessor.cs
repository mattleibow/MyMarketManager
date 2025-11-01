using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyMarketManager.Data.Entities;

namespace MyMarketManager.Data.Services;

/// <summary>
/// Service responsible for processing product images and generating vector embeddings.
/// </summary>
public class ImageVectorizationProcessor
{
    private readonly MyMarketManagerDbContext _context;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _imageEmbeddingGenerator;
    private readonly ILogger<ImageVectorizationProcessor> _logger;

    public ImageVectorizationProcessor(
        MyMarketManagerDbContext context,
        [FromKeyedServices("image")] IEmbeddingGenerator<string, Embedding<float>> imageEmbeddingGenerator,
        ILogger<ImageVectorizationProcessor> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _imageEmbeddingGenerator = imageEmbeddingGenerator ?? throw new ArgumentNullException(nameof(imageEmbeddingGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes all product images that don't have vector embeddings yet.
    /// </summary>
    public async Task ProcessPendingImagesAsync(CancellationToken cancellationToken = default)
    {
        // Find all product photos without vector embeddings
        var pendingPhotos = await _context.ProductPhotos
            .Where(p => p.VectorEmbedding == null)
            .ToListAsync(cancellationToken);

        if (pendingPhotos.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Found {Count} images pending vectorization", pendingPhotos.Count);

        var processedCount = 0;

        foreach (var photo in pendingPhotos)
        {
            try
            {
                _logger.LogInformation("Processing image {PhotoId} - {Url}", photo.Id, photo.Url);
                await ProcessImageAsync(photo, cancellationToken);
                processedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing image {PhotoId}", photo.Id);
                // Continue processing other images even if one fails
            }
        }

        _logger.LogInformation("Processed {Count} images", processedCount);
    }

    /// <summary>
    /// Processes a single product image to generate vector embeddings.
    /// </summary>
    public async Task ProcessImageAsync(ProductPhoto photo, CancellationToken cancellationToken = default)
    {
        try
        {
            // Vectorize image for similarity search
            var result = await _imageEmbeddingGenerator.GenerateAsync([photo.Url], cancellationToken: cancellationToken);
            var embedding = result.FirstOrDefault();

            if (embedding != null)
            {
                // Store vector
                photo.VectorEmbedding = embedding.Vector.ToArray();

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Successfully processed image {PhotoId}. Vector dimensions: {Dimensions}",
                    photo.Id, photo.VectorEmbedding.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process image {PhotoId}", photo.Id);
            throw;
        }
    }
}
