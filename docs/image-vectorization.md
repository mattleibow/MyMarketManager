# Image Vectorization

The image vectorization system uses Azure Computer Vision AI to generate vector embeddings for product photos, enabling AI-powered image similarity search and semantic search capabilities.

## Overview

Product photos are processed by the background processing system to generate 1024-dimensional vector embeddings using Azure Computer Vision's multimodal embedding model. These embeddings enable:

- **Similarity Search** - Find visually similar products
- **Semantic Search** - Search products by image or description
- **Content-Based Discovery** - Recommend products based on visual features

## Architecture

### Components

**MyMarketManager.AI Library**
- `ImageEmbeddingGenerator` - Generates embeddings for image URLs
- `TextEmbeddingGenerator` - Generates embeddings for text queries  
- `AzureComputerVisionEmbeddingGenerator` - Base class for embedding generators
- `Extensions` - DI registration methods with keyed services

**MyMarketManager.Processing.AI Library**
- `ImageVectorizationHandler` - Background handler that processes photos
- `ImageVectorizationWorkItem` - Work item representing a photo to vectorize

### Integration

The vectorization system integrates with the background processing system from PR #64:

```csharp
builder.Services.AddBackgroundProcessing()
    .AddHandler<ImageVectorizationHandler>(
        name: "ImageVectorization",
        maxItemsPerCycle: 10,
        purpose: WorkItemHandlerPurpose.Internal);
```

The handler:
1. Fetches product photos without vector embeddings (up to 10 per cycle)
2. Calls Azure Computer Vision API to generate embeddings
3. Stores the resulting 1024-dimensional vectors in the database

## Configuration

### Azure Computer Vision Setup

1. **Create Azure Computer Vision Resource**
   - Must be in a [supported region](https://learn.microsoft.com/en-us/azure/ai-services/computer-vision/overview-image-analysis#region-availability) for multimodal embeddings
   - API version: 2024-02-01
   - Model version: 2023-04-15 (multilingual support for 102 languages)

2. **Configure AppHost** (already done)
   - Azure AI Foundry resource is provisioned via Aspire
   - Connection passed to WebApp automatically

3. **Set Configuration** in `appsettings.json` or User Secrets:

```json
{
  "AzureAI": {
    "Endpoint": "https://<your-resource>.cognitiveservices.azure.com",
    "ApiKey": "<your-api-key>"
  }
}
```

### Service Registration

The embedding generators are registered as keyed services:

```csharp
// In Program.cs
builder.Services.AddAzureComputerVisionEmbeddings(endpoint, apiKey);

// Access via keyed services
[FromKeyedServices("image")] IEmbeddingGenerator<string, Embedding<float>> imageGenerator
[FromKeyedServices("text")] IEmbeddingGenerator<string, Embedding<float>> textGenerator
```

## Database Schema

### ProductPhoto Entity

```csharp
public class ProductPhoto : EntityBase
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Url { get; set; } = string.Empty;
    public string? Caption { get; set; }
    
    // 1024-dimensional vector embedding
    // Stored as comma-separated string in database
    public float[]? VectorEmbedding { get; set; }
}
```

The `VectorEmbedding` property:
- Type: `float[]` (1024 dimensions)
- Storage: Comma-separated string via EF Core value converter
- Database: TEXT column (compatible with SQL Server and SQLite)
- Size: ~4KB per image (1024 floats * ~4 bytes per float)

### EF Core Configuration

```csharp
// In MyMarketManagerDbContext.OnModelCreating()
modelBuilder.Entity<ProductPhoto>()
    .Property(p => p.VectorEmbedding)
    .HasConversion(
        v => v == null ? null : string.Join(",", v.Select(f => f.ToString("R"))),
        v => v == null ? null : v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(float.Parse).ToArray());
```

## Usage

### Automatic Vectorization

Photos are automatically vectorized by the background processing system:

1. Upload product photo (sets `ProductPhoto.Url`)
2. Leave `VectorEmbedding` as null
3. Background handler picks it up in next cycle
4. Embedding is generated and stored

### Manual Triggering

To process specific photos immediately:

```csharp
var handler = serviceProvider.GetRequiredService<ImageVectorizationHandler>();
var workItem = new ImageVectorizationWorkItem(photo);
await handler.ProcessAsync(workItem, cancellationToken);
```

### Similarity Search (Future)

Once vectors are generated, implement similarity search using cosine similarity:

```csharp
using System.Numerics.Tensors;

public async Task<List<Product>> FindSimilarProducts(float[] queryVector, int topK)
{
    var photos = await context.ProductPhotos
        .Where(p => p.VectorEmbedding != null)
        .Include(p => p.Product)
        .ToListAsync();
    
    var similarities = photos
        .Select(p => new
        {
            Photo = p,
            Similarity = TensorPrimitives.CosineSimilarity(
                queryVector.AsSpan(),
                p.VectorEmbedding.AsSpan())
        })
        .OrderByDescending(x => x.Similarity)
        .Take(topK)
        .Select(x => x.Photo.Product)
        .ToList();
    
    return similarities;
}
```

## Performance

**Processing Rate**
- Max 10 images per cycle (configurable)
- Default cycle interval: 5 minutes
- Throughput: ~120 images/hour

**API Limits**
- Azure Computer Vision: Check your tier limits
- HttpClient handler lifetime: 5 minutes (automatic recycling)
- Request timeout: 100 seconds

**Storage**
- Vector size: 1024 floats × 4 bytes = ~4KB per image
- 1000 products: ~4MB
- 10,000 products: ~40MB

## Testing

### Unit Tests

Tests are in `MyMarketManager.AI.Tests`:

```bash
dotnet test tests/MyMarketManager.AI.Tests
```

Test coverage:
- Service registration with keyed services
- HttpClient configuration
- Metadata properties
- Singleton lifecycle

### Integration Testing

To test with real Azure Computer Vision:

1. Set up test credentials in User Secrets
2. Create test photos with public URLs
3. Run handler manually:

```csharp
[Fact]
public async Task Handler_ProcessesPhoto()
{
    // Arrange
    var photo = new ProductPhoto 
    { 
        Url = "https://example.com/test.jpg",
        VectorEmbedding = null
    };
    context.ProductPhotos.Add(photo);
    await context.SaveChangesAsync();
    
    var workItem = new ImageVectorizationWorkItem(photo);
    
    // Act
    await handler.ProcessAsync(workItem, CancellationToken.None);
    
    // Assert
    Assert.NotNull(photo.VectorEmbedding);
    Assert.Equal(1024, photo.VectorEmbedding.Length);
}
```

## Troubleshooting

**No embeddings generated**
- Check Azure Computer Vision endpoint and API key
- Verify image URLs are publicly accessible
- Check logs for API errors

**Slow processing**
- Increase `maxItemsPerCycle` (default: 10)
- Reduce poll interval (default: 5 minutes)
- Check Azure Computer Vision tier limits

**Storage issues**
- Vectors are ~4KB each, ensure adequate database storage
- Consider archiving or cleanup for old/unused products

## Future Enhancements

Potential additions to the vectorization system:

1. **Search Service** - `ProductImageSearchService` for similarity queries
2. **UI Components** - Image upload and visual search interface
3. **GraphQL Queries** - Expose search operations via GraphQL
4. **Scalability** - Azure Cognitive Search or PostgreSQL pgvector for large catalogs
5. **Batch Optimization** - Process multiple images per API call
6. **Caching** - Cache frequently queried vectors

## References

- [Azure Computer Vision Image Embeddings](https://learn.microsoft.com/en-us/azure/ai-services/computer-vision/how-to/image-retrieval)
- [Microsoft.Extensions.AI](https://www.nuget.org/packages/Microsoft.Extensions.AI.Abstractions)
- [Background Processing System](background-processing.md)
- [Architecture](architecture.md)
