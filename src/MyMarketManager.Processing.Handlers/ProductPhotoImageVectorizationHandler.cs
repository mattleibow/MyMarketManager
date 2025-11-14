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
    IHttpClientFactory httpClientFactory,
    ILogger<ProductPhotoImageVectorizationHandler> logger)
    : ImageVectorizationHandler<UriWorkItem>(embeddingGenerator, httpClientFactory, logger)
{
    public override async Task<IReadOnlyCollection<UriWorkItem>> FetchNextAsync(
        int maxItems,
        CancellationToken cancellationToken)
    {
        // Fetch photos without vector embeddings (no tracking - just IDs, URLs, and MIME types)
        var pendingPhotos = await context.ProductPhotos
            .AsNoTracking()
            .Where(p => p.VectorEmbedding == null)
            .Take(maxItems)
            .Select(p => new UriWorkItem(p.Id, p.Url, p.MimeType))
            .ToListAsync(cancellationToken);

        return pendingPhotos;
    }

    protected override async Task StoreEmbeddingAsync(
        Guid imageId,
        float[] embedding,
        CancellationToken cancellationToken)
    {
        // Reload the entity to ensure it's tracked in the current DbContext
        var photo = await context.ProductPhotos.FindAsync([imageId], cancellationToken);

        if (photo is null)
        {
            throw new InvalidOperationException($"ProductPhoto {imageId} not found");
        }

        photo.VectorEmbedding = embedding;
        await context.SaveChangesAsync(cancellationToken);
    }
}
