using MyMarketManager.Data.Entities;

namespace MyMarketManager.Data.Processing;

/// <summary>
/// Work item representing a product photo that needs vectorization.
/// </summary>
public class ImageVectorizationWorkItem : IWorkItem
{
    public ImageVectorizationWorkItem(ProductPhoto photo)
    {
        Photo = photo ?? throw new ArgumentNullException(nameof(photo));
    }

    public Guid Id => Photo.Id;

    public ProductPhoto Photo { get; }
}
