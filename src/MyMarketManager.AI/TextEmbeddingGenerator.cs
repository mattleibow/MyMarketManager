namespace MyMarketManager.AI;

/// <summary>
/// Azure Computer Vision embedding generator for text queries.
/// Implements IEmbeddingGenerator for semantic text search using the retrieval:vectorizeText endpoint.
/// </summary>
public class TextEmbeddingGenerator(
    IHttpClientFactory httpClientFactory,
    string httpClientName,
    string modelVersion = "2023-04-15")
    : AzureComputerVisionEmbeddingGenerator(
        "Text",
        "retrieval:vectorizeText",
        httpClientFactory,
        httpClientName,
        modelVersion)
{
    protected override object CreateRequestPayload(string value) => new { text = value };
}
