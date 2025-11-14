using HotChocolate.Types;
using MyMarketManager.Data.Entities;

namespace MyMarketManager.WebApp.GraphQL.Types;

/// <summary>
/// Configures the GraphQL shape for <see cref="ProductPhoto"/>.
/// Explicitly exposes supported metadata and skips vector embeddings that are not serialized.
/// </summary>
public class ProductPhotoType : ObjectType<ProductPhoto>
{
    protected override void Configure(IObjectTypeDescriptor<ProductPhoto> descriptor)
    {
        descriptor.Description("Represents a product image stored for a product.");
        descriptor.Field(photo => photo.VectorEmbedding).Ignore();
        descriptor.Field(photo => photo.MimeType)
            .Description("MIME type of the stored image, such as image/jpeg or image/png.");
    }
}
