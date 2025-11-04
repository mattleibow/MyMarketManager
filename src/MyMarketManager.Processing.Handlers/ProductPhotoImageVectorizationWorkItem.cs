using MyMarketManager.Data.Entities;

namespace MyMarketManager.Processing.Handlers;

/// <summary>
/// Work item representing a product photo that needs vectorization.
/// </summary>
public class ProductPhotoImageVectorizationWorkItem : IWorkItem
{
    public ProductPhotoImageVectorizationWorkItem(ProductPhoto photo)
    {
        Photo = photo ?? throw new ArgumentNullException(nameof(photo));
    }

    public Guid Id => Photo.Id;

    public ProductPhoto Photo { get; }
}
