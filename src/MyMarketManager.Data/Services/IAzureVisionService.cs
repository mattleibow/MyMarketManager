namespace MyMarketManager.Data.Services;

/// <summary>
/// Service for Azure AI Vision operations including image analysis and vectorization.
/// </summary>
public interface IAzureVisionService
{
    /// <summary>
    /// Analyzes an image to generate captions and tags.
    /// </summary>
    /// <param name="imageUrl">URL of the image to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Analysis result containing caption and tags.</returns>
    Task<ImageAnalysisResult> AnalyzeImageAsync(string imageUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Vectorizes an image into a 1024-dimensional embedding.
    /// </summary>
    /// <param name="imageUrl">URL of the image to vectorize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>1024-dimensional float array representing the image.</returns>
    Task<float[]> VectorizeImageAsync(string imageUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Vectorizes text into a 1024-dimensional embedding.
    /// </summary>
    /// <param name="text">Text to vectorize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>1024-dimensional float array representing the text.</returns>
    Task<float[]> VectorizeTextAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates cosine similarity between two vectors.
    /// </summary>
    /// <param name="vector1">First vector.</param>
    /// <param name="vector2">Second vector.</param>
    /// <returns>Similarity score between 0 and 1.</returns>
    float CalculateCosineSimilarity(float[] vector1, float[] vector2);
}

/// <summary>
/// Result of image analysis containing description and tags.
/// </summary>
public class ImageAnalysisResult
{
    public string Caption { get; set; } = string.Empty;
    public float CaptionConfidence { get; set; }
    public List<ImageTag> Tags { get; set; } = new();
}

/// <summary>
/// Tag detected in an image.
/// </summary>
public class ImageTag
{
    public string Name { get; set; } = string.Empty;
    public float Confidence { get; set; }
}
