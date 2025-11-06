using MyMarketManager.Data.Entities;

namespace MyMarketManager.Processing.Handlers;

/// <summary>
/// Work item representing a product photo that needs vectorization.
/// </summary>
public class ProductPhotoImageVectorizationWorkItem(ProductPhoto photo) : ImageVectorizationWorkItem(photo.Id, photo.Url)
{
    public ProductPhoto Photo { get; } = photo ?? throw new ArgumentNullException(nameof(photo));
}
