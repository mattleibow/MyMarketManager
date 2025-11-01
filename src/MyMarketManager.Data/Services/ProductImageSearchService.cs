using System.Numerics.Tensors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyMarketManager.Data.Entities;

namespace MyMarketManager.Data.Services;

/// <summary>
/// Service for searching product images using vector similarity.
/// </summary>
public class ProductImageSearchService
{
    private readonly MyMarketManagerDbContext _context;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _imageEmbeddingGenerator;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _textEmbeddingGenerator;
    private readonly ILogger<ProductImageSearchService> _logger;

    public ProductImageSearchService(
        MyMarketManagerDbContext context,
        [FromKeyedServices("image")] IEmbeddingGenerator<string, Embedding<float>> imageEmbeddingGenerator,
        [FromKeyedServices("text")] IEmbeddingGenerator<string, Embedding<float>> textEmbeddingGenerator,
        ILogger<ProductImageSearchService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _imageEmbeddingGenerator = imageEmbeddingGenerator ?? throw new ArgumentNullException(nameof(imageEmbeddingGenerator));
        _textEmbeddingGenerator = textEmbeddingGenerator ?? throw new ArgumentNullException(nameof(textEmbeddingGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Searches for product images similar to the given image URL.
    /// Note: For large datasets, consider implementing pagination or using a vector database.
    /// </summary>
    public async Task<List<ProductImageSearchResult>> SearchByImageAsync(
        string imageUrl,
        int maxResults = 10,
        float similarityThreshold = 0.7f,
        CancellationToken cancellationToken = default)
    {
        // Vectorize the search image
        var result = await _imageEmbeddingGenerator.GenerateAsync([imageUrl], cancellationToken: cancellationToken);
        var searchEmbedding = result.FirstOrDefault();

        if (searchEmbedding == null)
        {
            return new List<ProductImageSearchResult>();
        }

        var searchVector = searchEmbedding.Vector.ToArray();

        // Get vectorized images (for large datasets, consider pagination or filtering)
        var allPhotos = await _context.ProductPhotos
            .Include(p => p.Product)
            .Where(p => p.VectorEmbedding != null)
            .Take(1000) // Limit to prevent memory issues with large datasets
            .ToListAsync(cancellationToken);

        // Calculate similarity scores
        var results = new List<ProductImageSearchResult>();

        foreach (var photo in allPhotos)
        {
            try
            {
                if (photo.VectorEmbedding == null) continue;

                var similarity = TensorPrimitives.CosineSimilarity(searchVector, photo.VectorEmbedding);

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
    /// Note: For large datasets, consider implementing pagination or using a vector database.
    /// </summary>
    public async Task<List<ProductImageSearchResult>> SearchByTextAsync(
        string searchText,
        int maxResults = 10,
        float similarityThreshold = 0.6f,
        CancellationToken cancellationToken = default)
    {
        // Vectorize the search text
        var result = await _textEmbeddingGenerator.GenerateAsync([searchText], cancellationToken: cancellationToken);
        var searchEmbedding = result.FirstOrDefault();

        if (searchEmbedding == null)
        {
            return new List<ProductImageSearchResult>();
        }

        var searchVector = searchEmbedding.Vector.ToArray();

        // Get vectorized images (for large datasets, consider pagination or filtering)
        var allPhotos = await _context.ProductPhotos
            .Include(p => p.Product)
            .Where(p => p.VectorEmbedding != null)
            .Take(1000) // Limit to prevent memory issues with large datasets
            .ToListAsync(cancellationToken);

        // Calculate similarity scores
        var results = new List<ProductImageSearchResult>();

        foreach (var photo in allPhotos)
        {
            try
            {
                if (photo.VectorEmbedding == null) continue;

                var similarity = TensorPrimitives.CosineSimilarity(searchVector, photo.VectorEmbedding);

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
