namespace MyMarketManager.Data.Processing;

/// <summary>
/// Represents a batch of images to be vectorized.
/// This is a lightweight work item that doesn't require database persistence.
/// The actual work is finding and processing all pending ProductPhoto records.
/// </summary>
public class ImageVectorizationWorkItem : IWorkItem
{
    /// <summary>
    /// Creates a new image vectorization work item.
    /// </summary>
    /// <param name="processorName">The name of the processor to use (typically "ImageVectorization").</param>
    public ImageVectorizationWorkItem(string processorName)
    {
        Id = Guid.NewGuid();
        ProcessorName = processorName ?? throw new ArgumentNullException(nameof(processorName));
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc/>
    public Guid Id { get; }

    /// <inheritdoc/>
    public string ProcessorName { get; }

    /// <summary>
    /// When this work item was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Optional limit on the number of images to process in this batch.
    /// If null, processes all pending images.
    /// </summary>
    public int? MaxImages { get; set; }
}
