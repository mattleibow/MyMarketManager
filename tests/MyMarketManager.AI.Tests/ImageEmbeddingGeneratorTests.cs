using Microsoft.Extensions.AI;
using Moq;
using System.Net;

namespace MyMarketManager.AI.Tests;

public class ImageEmbeddingGeneratorTests
{
    [Fact]
    public void Constructor_SetsMetadata()
    {
        // Arrange
        var httpClientFactory = Mock.Of<IHttpClientFactory>();

        // Act
        var generator = new ImageEmbeddingGenerator(httpClientFactory, "test-client");

        // Assert
        Assert.NotNull(generator.Metadata);
        Assert.Contains("Image", generator.Metadata.ProviderName);
    }

    [Fact]
    public void GetService_ReturnsMetadata()
    {
        // Arrange
        var httpClientFactory = Mock.Of<IHttpClientFactory>();
        var generator = new ImageEmbeddingGenerator(httpClientFactory, "test-client");

        // Act
        var metadata = generator.GetService(typeof(EmbeddingGeneratorMetadata));

        // Assert
        Assert.NotNull(metadata);
        Assert.IsType<EmbeddingGeneratorMetadata>(metadata);
    }

    [Fact]
    public void GetService_ReturnsNull_ForUnknownType()
    {
        // Arrange
        var httpClientFactory = Mock.Of<IHttpClientFactory>();
        var generator = new ImageEmbeddingGenerator(httpClientFactory, "test-client");

        // Act
        var result = generator.GetService(typeof(string));

        // Assert
        Assert.Null(result);
    }
}
