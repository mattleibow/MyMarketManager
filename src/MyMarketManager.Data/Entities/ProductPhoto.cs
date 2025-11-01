using System.ComponentModel.DataAnnotations;

namespace MyMarketManager.Data.Entities;

/// <summary>
/// Stores one or more images associated with a product with AI-generated vector embeddings for similarity search.
/// </summary>
public class ProductPhoto : EntityBase
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Required]
    public string Url { get; set; } = string.Empty;
    
    /// <summary>
    /// Caption describing the image (user-provided or AI-generated).
    /// </summary>
    public string? Caption { get; set; }
    
    /// <summary>
    /// 1024-dimensional vector embedding from Azure AI Foundry multimodal embeddings.
    /// Used for image similarity search and semantic search.
    /// Stored as binary in SQL Server, ignored in SQLite.
    /// </summary>
    public float[]? VectorEmbedding { get; set; }
}
