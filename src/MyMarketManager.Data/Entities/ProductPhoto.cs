using System.ComponentModel.DataAnnotations;

namespace MyMarketManager.Data.Entities;

/// <summary>
/// Stores one or more images associated with a product with AI-generated analysis and vector embeddings for similarity search.
/// </summary>
public class ProductPhoto : EntityBase
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Required]
    public string Url { get; set; } = string.Empty;
    
    /// <summary>
    /// User-provided or AI-generated caption describing the image.
    /// </summary>
    public string? Caption { get; set; }
    
    /// <summary>
    /// AI-generated description of the image content.
    /// </summary>
    public string? AiDescription { get; set; }
    
    /// <summary>
    /// Comma-separated AI-generated tags for the image.
    /// </summary>
    public string? AiTags { get; set; }
    
    /// <summary>
    /// JSON-encoded 1024-dimensional vector embedding from Azure AI Vision multimodal embeddings.
    /// Used for image similarity search and semantic search.
    /// </summary>
    public string? VectorEmbedding { get; set; }
    
    /// <summary>
    /// Date when the image was analyzed and vectors were generated.
    /// </summary>
    public DateTimeOffset? VectorizedAt { get; set; }
}
