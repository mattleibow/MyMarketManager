using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyMarketManager.Data;

namespace MyMarketManager.Processing.Handlers;

/// <summary>
/// Handler that fetches product photos without vector embeddings and generates embeddings.
/// </summary>
public class ProductPhotoImageVectorizationHandler : ImageVectorizationHandler<ProductPhotoImageVectorizationWorkItem>
{
    private readonly MyMarketManagerDbContext _context;

    public ProductPhotoImageVectorizationHandler(
        MyMarketManagerDbContext context,
        [FromKeyedServices("image")] IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        ILogger<ProductPhotoImageVectorizationHandler> logger)
        : base(embeddingGenerator, logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public override async Task<IReadOnlyCollection<ProductPhotoImageVectorizationWorkItem>> FetchNextAsync(
        int maxItems,
        CancellationToken cancellationToken)
    {
        // Fetch photos without vector embeddings
        var pendingPhotos = await _context.ProductPhotos
            .Where(p => p.VectorEmbedding == null)
            .Take(maxItems)
            .ToListAsync(cancellationToken);

        return pendingPhotos
            .Select(p => new ProductPhotoImageVectorizationWorkItem(p))
            .ToList();
    }

    protected override string GetImageUrl(ProductPhotoImageVectorizationWorkItem workItem)
    {
        return workItem.Photo.Url;
    }

    protected override async Task StoreEmbeddingAsync(
        ProductPhotoImageVectorizationWorkItem workItem,
        float[] embedding,
        CancellationToken cancellationToken)
    {
        workItem.Photo.VectorEmbedding = embedding;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
