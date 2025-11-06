using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using MyMarketManager.Data;

namespace MyMarketManager.Processing.Handlers;

/// <summary>
/// Handler that fetches product photos without vector embeddings and generates embeddings.
/// </summary>
public class ProductPhotoImageVectorizationHandler(
    MyMarketManagerDbContext context,
    IEmbeddingGenerator<DataContent, Embedding<float>> embeddingGenerator,
    ILogger<ProductPhotoImageVectorizationHandler> logger)
    : ImageVectorizationHandler<ProductPhotoImageVectorizationWorkItem>(embeddingGenerator, logger)
{
    public override async Task<IReadOnlyCollection<ProductPhotoImageVectorizationWorkItem>> FetchNextAsync(
        int maxItems,
        CancellationToken cancellationToken)
    {
        // Fetch photos without vector embeddings
        var pendingPhotos = await context.ProductPhotos
            .Where(p => p.VectorEmbedding == null)
            .Take(maxItems)
            .ToListAsync(cancellationToken);

        return [.. pendingPhotos.Select(p => new ProductPhotoImageVectorizationWorkItem(p))];
    }

    protected override async Task StoreEmbeddingAsync(
        ProductPhotoImageVectorizationWorkItem workItem,
        float[] embedding,
        CancellationToken cancellationToken)
    {
        // Entity is already tracked in this scope's DbContext from FetchNextAsync
        workItem.Photo.VectorEmbedding = embedding;

        await context.SaveChangesAsync(cancellationToken);
    }
}

