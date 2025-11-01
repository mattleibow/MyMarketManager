# Product Image Vectorization and Search

## Overview

The Product Image Vectorization and Search feature enables AI-powered image search for products using Azure AI Vision 4.0 multimodal embeddings. Images are automatically analyzed to generate descriptions, tags, and 1024-dimensional vector embeddings that enable similarity-based search.

## Features

### 1. Automatic Image Analysis
- **AI-Generated Descriptions**: Natural language captions describing image content
- **Auto-Tagging**: Automatic generation of relevant tags for products
- **Vector Embeddings**: 1024-dimensional vectors for similarity search
- **Background Processing**: Automatic processing of new images every 10 minutes

### 2. Multi-Modal Search
- **Image Search**: Upload or provide a URL to find visually similar products
- **Text Search**: Describe what you're looking for in natural language
- **Tag Filtering**: Filter products by AI-generated or custom tags
- **Adjustable Thresholds**: Control search sensitivity

### 3. Smart Similarity Matching
- **Cosine Similarity**: Industry-standard vector comparison
- **Relevance Scoring**: Visual similarity scores from 0-100%
- **Ranked Results**: Results ordered by similarity score

## Architecture

### Components

1. **Azure AI Vision Service** (`AzureVisionService`)
   - Analyzes images using Azure AI Vision 4.0 API
   - Generates captions and tags
   - Creates 1024-dimensional vector embeddings
   - Vectorizes text queries for semantic search

2. **Image Vectorization Processor** (`ImageVectorizationProcessor`)
   - Processes images without vectors
   - Searches by image, text, or tags
   - Calculates similarity scores

3. **Background Service** (`ImageVectorizationBackgroundService`)
   - Runs every 10 minutes
   - Automatically processes pending images
   - No manual intervention required

4. **Search UI** (`ProductImageSearch.razor`)
   - Three search modes: Image, Text, Tags
   - Adjustable similarity thresholds
   - Visual results with similarity scores

### Database Schema

The `ProductPhoto` entity was extended with the following fields:

```csharp
public class ProductPhoto : EntityBase
{
    // Existing fields
    public Guid ProductId { get; set; }
    public string Url { get; set; }
    public string? Caption { get; set; }
    
    // New AI fields
    public string? AiDescription { get; set; }        // AI-generated caption
    public string? AiTags { get; set; }               // Comma-separated tags
    public string? VectorEmbedding { get; set; }      // JSON-encoded vector (1024 dims)
    public DateTimeOffset? VectorizedAt { get; set; } // Processing timestamp
}
```

## Setup

### Prerequisites

1. **Azure AI Vision Resource**: Create an Azure AI Vision resource in a [supported region](https://learn.microsoft.com/en-us/azure/ai-services/computer-vision/overview-image-analysis#region-availability)
2. **API Access**: Obtain endpoint and API key from Azure portal

### Configuration

Add the following to `appsettings.json` or use environment variables:

```json
{
  "AzureVision": {
    "Endpoint": "https://your-vision-resource.cognitiveservices.azure.com/",
    "ApiKey": "your-api-key-here"
  }
}
```

**Environment Variables** (alternative):
```bash
AzureVision__Endpoint=https://your-vision-resource.cognitiveservices.azure.com/
AzureVision__ApiKey=your-api-key-here
```

### Database Migration

The database migration is applied automatically on startup. To apply manually:

```bash
dotnet ef database update --project src/MyMarketManager.Data
```

## Usage

### Adding Product Images

1. Navigate to a product page
2. Add image URLs to the product
3. Images are queued for automatic processing
4. Within 10 minutes, images will be analyzed and vectorized

### Searching for Products

1. Navigate to **Image Search** from the main menu
2. Choose a search mode:
   - **Image**: Paste a product image URL
   - **Text**: Describe what you're looking for (e.g., "red dress with flowers")
   - **Tags**: Enter comma-separated tags (e.g., "dress, red, cotton")
3. Adjust the similarity threshold if needed
4. Click **Search**
5. Results show similarity scores and AI-generated information

### Search Examples

**Image Search:**
- Upload a product photo to find identical or similar items
- Useful for finding duplicate products or visual matches

**Text Search:**
- "blue denim jacket"
- "floral summer dress"
- "leather handbag with gold hardware"

**Tag Search:**
- "outdoor, waterproof, jacket"
- "cotton, striped, shirt"

## Technical Details

### Vector Embeddings

- **Dimensions**: 1024-dimensional float arrays
- **Model**: Azure AI Vision multimodal embeddings (2023-04-15)
- **Storage**: JSON-encoded strings in SQL Server
- **Similarity Metric**: Cosine similarity
- **Language Support**: Multilingual (102 languages)

### Processing Pipeline

```
1. User adds image URL to product
   ↓
2. Image saved to ProductPhoto table (VectorEmbedding = null)
   ↓
3. Background service detects pending images (every 10 minutes)
   ↓
4. For each pending image:
   a. Call Azure AI Vision Analyze API → Get caption & tags
   b. Call Azure AI Vision Vectorize API → Get 1024-dim vector
   c. Save results to database
   ↓
5. Image ready for search
```

### Search Algorithm

**Image/Text Search:**
1. Vectorize search query (image or text)
2. Load all vectorized product images
3. Calculate cosine similarity with each image
4. Filter by threshold (default: 0.7 for images, 0.6 for text)
5. Sort by similarity (descending)
6. Return top N results (default: 10-20)

**Tag Search:**
1. Split search tags by comma
2. Match against AI-generated tags (case-insensitive)
3. Calculate match score (matched tags / total tags)
4. Sort by match score

### Performance Considerations

- **Batch Size**: Processes up to 10 images per cycle
- **API Rate Limits**: Uses standard tier for higher throughput
- **Background Processing**: Non-blocking, runs every 10 minutes
- **Search Performance**: O(n) similarity calculation where n = number of vectorized images
- **Optimization**: For large datasets, consider:
  - Indexing vectors in a vector database (e.g., Azure Cognitive Search, PostgreSQL pgvector)
  - Caching frequently accessed vectors
  - Implementing approximate nearest neighbor (ANN) algorithms

## API Reference

### IAzureVisionService

```csharp
// Analyze image to get caption and tags
Task<ImageAnalysisResult> AnalyzeImageAsync(string imageUrl, CancellationToken cancellationToken = default);

// Vectorize image (1024 dimensions)
Task<float[]> VectorizeImageAsync(string imageUrl, CancellationToken cancellationToken = default);

// Vectorize text query (1024 dimensions)
Task<float[]> VectorizeTextAsync(string text, CancellationToken cancellationToken = default);

// Calculate cosine similarity (0-1)
float CalculateCosineSimilarity(float[] vector1, float[] vector2);
```

### ImageVectorizationProcessor

```csharp
// Process all pending images
Task<int> ProcessPendingImagesAsync(CancellationToken cancellationToken = default);

// Process a single image
Task ProcessImageAsync(ProductPhoto photo, CancellationToken cancellationToken = default);

// Search by image URL
Task<List<ProductImageSearchResult>> SearchByImageAsync(string imageUrl, int maxResults = 10, float similarityThreshold = 0.7f, CancellationToken cancellationToken = default);

// Search by text description
Task<List<ProductImageSearchResult>> SearchByTextAsync(string searchText, int maxResults = 10, float similarityThreshold = 0.6f, CancellationToken cancellationToken = default);

// Search by tags
Task<List<ProductImageSearchResult>> SearchByTagsAsync(List<string> tags, CancellationToken cancellationToken = default);
```

## Troubleshooting

### Images Not Being Processed

1. **Check Azure AI Vision Configuration**: Verify endpoint and API key are correct
2. **Check Logs**: Look for errors in background service logs
3. **Verify Region**: Ensure your Azure AI Vision resource is in a [supported region](https://learn.microsoft.com/en-us/azure/ai-services/computer-vision/overview-image-analysis#region-availability)
4. **Check API Quota**: Ensure you haven't exceeded API rate limits

### Low Search Quality

1. **Adjust Threshold**: Lower the similarity threshold to see more results
2. **Check Image Quality**: Ensure images are high-quality and representative
3. **Verify Vectorization**: Check that VectorizedAt is populated
4. **Test with Known Images**: Upload the same image and search to verify 100% match

### API Errors

Common errors and solutions:

| Error | Solution |
|-------|----------|
| `401 Unauthorized` | Check API key is correct |
| `403 Forbidden` | Verify endpoint URL matches resource region |
| `429 Too Many Requests` | Slow down processing or upgrade to higher tier |
| `Region not supported` | Create new resource in supported region |

## Future Enhancements

- [ ] Blob storage integration for image uploads
- [ ] Batch processing UI to manually trigger vectorization
- [ ] Vector database integration for faster search (Azure Cognitive Search, Pinecone)
- [ ] Advanced filtering (combine tags + similarity)
- [ ] Image quality assessment
- [ ] Duplicate image detection
- [ ] GraphQL API for search operations
- [ ] Mobile app integration

## References

- [Azure AI Vision Documentation](https://learn.microsoft.com/en-us/azure/ai-services/computer-vision/)
- [Multimodal Embeddings](https://learn.microsoft.com/en-us/azure/ai-services/computer-vision/concept-image-retrieval)
- [Image Analysis 4.0](https://learn.microsoft.com/en-us/azure/ai-services/computer-vision/how-to/call-analyze-image-40)
- [Cosine Similarity](https://en.wikipedia.org/wiki/Cosine_similarity)
