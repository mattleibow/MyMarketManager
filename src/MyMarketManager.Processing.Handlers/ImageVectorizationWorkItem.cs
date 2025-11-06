namespace MyMarketManager.Processing.Handlers;

/// <summary>
/// Base class for image vectorization work items.
/// Contains the ID and URL needed for vectorization.
/// </summary>
public abstract class ImageVectorizationWorkItem : IWorkItem
{
    protected ImageVectorizationWorkItem(Guid id, string imageUrl)
    {
        Id = id;
        ImageUrl = imageUrl ?? throw new ArgumentNullException(nameof(imageUrl));
    }

    public Guid Id { get; }
    
    public string ImageUrl { get; }
}
