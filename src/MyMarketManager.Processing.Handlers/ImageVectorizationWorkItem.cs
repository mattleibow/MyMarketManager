using Microsoft.Extensions.AI;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace MyMarketManager.Processing.Handlers;

/// <summary>
/// Base class for image vectorization work items.
/// Contains the ID and URL needed for vectorization.
/// </summary>
public abstract class ImageVectorizationWorkItem : IWorkItem
{
    protected ImageVectorizationWorkItem(Guid id, string imageUrl, string mimeType)
    {
        Id = id;
        ImageUrl = imageUrl ?? throw new ArgumentNullException(nameof(imageUrl));
        MimeType = mimeType ?? throw new ArgumentNullException(nameof(mimeType));
    }

    public Guid Id { get; }
    
    public string ImageUrl { get; }
    
    public string MimeType { get; }
}
