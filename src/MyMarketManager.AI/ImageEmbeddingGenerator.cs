namespace MyMarketManager.AI;

/// <summary>
/// Azure Computer Vision embedding generator for product images.
/// Implements IEmbeddingGenerator for image URLs using the retrieval:vectorizeImage endpoint.
/// </summary>
public class ImageEmbeddingGenerator(
    IHttpClientFactory httpClientFactory,
    string httpClientName,
    string modelVersion = "2023-04-15")
    : AzureComputerVisionEmbeddingGenerator(
        "Image",
        "retrieval:vectorizeImage",
        httpClientFactory,
        httpClientName,
        modelVersion)
{
    protected override object CreateRequestPayload(string value) => new { url = value };
}
