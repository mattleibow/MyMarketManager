using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMarketManager.Data.Entities;

namespace MyMarketManager.Data.Services;

/// <summary>
/// Service responsible for processing product images and generating AI analysis and vector embeddings.
/// </summary>
public class ImageVectorizationProcessor
{
    private readonly MyMarketManagerDbContext _context;
    private readonly IAzureVisionService _visionService;
    private readonly ILogger<ImageVectorizationProcessor> _logger;

    public ImageVectorizationProcessor(
        MyMarketManagerDbContext context,
        IAzureVisionService visionService,
        ILogger<ImageVectorizationProcessor> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _visionService = visionService ?? throw new ArgumentNullException(nameof(visionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes all product images that don't have vector embeddings yet.
    /// </summary>
    public async Task<int> ProcessPendingImagesAsync(CancellationToken cancellationToken = default)
    {
        // Find all product photos without vector embeddings
        var pendingPhotos = await _context.ProductPhotos
            .Where(p => p.VectorEmbedding == null)
            .Take(10) // Process in batches to avoid overwhelming the API
            .ToListAsync(cancellationToken);

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

        return processedCount;
    }

    /// <summary>
    /// Processes a single product image to generate AI analysis and vector embeddings.
    /// </summary>
    public async Task ProcessImageAsync(ProductPhoto photo, CancellationToken cancellationToken = default)
    {
        try
        {
            // Analyze image to get caption and tags
            var analysis = await _visionService.AnalyzeImageAsync(photo.Url, cancellationToken);

            // Vectorize image for similarity search
            var vector = await _visionService.VectorizeImageAsync(photo.Url, cancellationToken);

            // Update the photo with AI-generated data
            photo.AiDescription = analysis.Caption;
            photo.AiTags = string.Join(", ", analysis.Tags
                .OrderByDescending(t => t.Confidence)
                .Take(20) // Limit to top 20 tags
                .Select(t => t.Name));

            // Store vector as JSON array
            photo.VectorEmbedding = JsonSerializer.Serialize(vector);
            photo.VectorizedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully processed image {PhotoId}. Description: {Description}, Tags: {TagCount}",
                photo.Id, photo.AiDescription, analysis.Tags.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process image {PhotoId}", photo.Id);
            throw;
        }
    }

    /// <summary>
    /// Searches for product images similar to the given image URL.
    /// </summary>
    public async Task<List<ProductImageSearchResult>> SearchByImageAsync(
        string imageUrl,
        int maxResults = 10,
        float similarityThreshold = 0.7f,
        CancellationToken cancellationToken = default)
    {
        // Vectorize the search image
        var searchVector = await _visionService.VectorizeImageAsync(imageUrl, cancellationToken);

        // Get all vectorized images
        var allPhotos = await _context.ProductPhotos
            .Include(p => p.Product)
            .Where(p => p.VectorEmbedding != null)
            .ToListAsync(cancellationToken);

        // Calculate similarity scores
        var results = new List<ProductImageSearchResult>();

        foreach (var photo in allPhotos)
        {
            try
            {
                var photoVector = JsonSerializer.Deserialize<float[]>(photo.VectorEmbedding!);
                if (photoVector == null) continue;

                var similarity = _visionService.CalculateCosineSimilarity(searchVector, photoVector);

                if (similarity >= similarityThreshold)
                {
                    results.Add(new ProductImageSearchResult
                    {
                        ProductPhoto = photo,
                        Product = photo.Product,
                        SimilarityScore = similarity
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating similarity for photo {PhotoId}", photo.Id);
            }
        }

        // Return top N results ordered by similarity
        return results
            .OrderByDescending(r => r.SimilarityScore)
            .Take(maxResults)
            .ToList();
    }

    /// <summary>
    /// Searches for product images using semantic text search.
    /// </summary>
    public async Task<List<ProductImageSearchResult>> SearchByTextAsync(
        string searchText,
        int maxResults = 10,
        float similarityThreshold = 0.6f,
        CancellationToken cancellationToken = default)
    {
        // Vectorize the search text
        var searchVector = await _visionService.VectorizeTextAsync(searchText, cancellationToken);

        // Get all vectorized images
        var allPhotos = await _context.ProductPhotos
            .Include(p => p.Product)
            .Where(p => p.VectorEmbedding != null)
            .ToListAsync(cancellationToken);

        // Calculate similarity scores
        var results = new List<ProductImageSearchResult>();

        foreach (var photo in allPhotos)
        {
            try
            {
                var photoVector = JsonSerializer.Deserialize<float[]>(photo.VectorEmbedding!);
                if (photoVector == null) continue;

                var similarity = _visionService.CalculateCosineSimilarity(searchVector, photoVector);

                if (similarity >= similarityThreshold)
                {
                    results.Add(new ProductImageSearchResult
                    {
                        ProductPhoto = photo,
                        Product = photo.Product,
                        SimilarityScore = similarity
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating similarity for photo {PhotoId}", photo.Id);
            }
        }

        // Return top N results ordered by similarity
        return results
            .OrderByDescending(r => r.SimilarityScore)
            .Take(maxResults)
            .ToList();
    }

    /// <summary>
    /// Searches for products by tags.
    /// </summary>
    public async Task<List<ProductImageSearchResult>> SearchByTagsAsync(
        List<string> tags,
        CancellationToken cancellationToken = default)
    {
        // Find photos that contain any of the search tags
        var results = new List<ProductImageSearchResult>();

        var allPhotos = await _context.ProductPhotos
            .Include(p => p.Product)
            .Where(p => p.AiTags != null)
            .ToListAsync(cancellationToken);

        foreach (var photo in allPhotos)
        {
            var photoTags = photo.AiTags!.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var matchingTags = tags.Count(tag => photoTags.Any(pt => pt.Equals(tag, StringComparison.OrdinalIgnoreCase)));

            if (matchingTags > 0)
            {
                results.Add(new ProductImageSearchResult
                {
                    ProductPhoto = photo,
                    Product = photo.Product,
                    SimilarityScore = (float)matchingTags / tags.Count
                });
            }
        }

        return results
            .OrderByDescending(r => r.SimilarityScore)
            .ToList();
    }
}

/// <summary>
/// Result of a product image search.
/// </summary>
public class ProductImageSearchResult
{
    public ProductPhoto ProductPhoto { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public float SimilarityScore { get; set; }
}
