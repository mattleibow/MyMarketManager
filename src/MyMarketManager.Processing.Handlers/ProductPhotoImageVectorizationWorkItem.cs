using MyMarketManager.Data.Entities;

namespace MyMarketManager.Processing.Handlers;

/// <summary>
/// Work item representing a product photo that needs vectorization.
/// Contains the tracked entity - safe because each work item is processed in its own scope.
/// </summary>
public class ProductPhotoImageVectorizationWorkItem : ImageVectorizationWorkItem
{
    public ProductPhotoImageVectorizationWorkItem(ProductPhoto photo)
        : base(photo?.Id ?? throw new ArgumentNullException(nameof(photo)), photo.Url, photo.MimeType)
    {
        Photo = photo;
    }

    public ProductPhoto Photo { get; }
}
