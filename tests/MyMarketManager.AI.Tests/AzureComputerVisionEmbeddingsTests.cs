using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;
using MyMarketManager.AI;

namespace MyMarketManager.AI.Tests;

public class AzureComputerVisionEmbeddingsTests
{
    [Fact]
    public void AddAzureComputerVisionEmbeddings_RegistersImageAndTextGenerators()
    {
        // Arrange
        var services = new ServiceCollection();
        var endpoint = "https://test.cognitiveservices.azure.com";
        var apiKey = "test-api-key";

        // Act
        services.AddAzureComputerVisionEmbeddings(endpoint, apiKey);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var imageGenerator = serviceProvider.GetKeyedService<IEmbeddingGenerator<string, Embedding<float>>>("image");
        var textGenerator = serviceProvider.GetKeyedService<IEmbeddingGenerator<string, Embedding<float>>>("text");

        Assert.NotNull(imageGenerator);
        Assert.NotNull(textGenerator);
        Assert.IsType<ImageEmbeddingGenerator>(imageGenerator);
        Assert.IsType<TextEmbeddingGenerator>(textGenerator);
    }

    [Fact]
    public void AddAzureComputerVisionEmbeddings_RegistersHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();
        var endpoint = "https://test.cognitiveservices.azure.com";
        var apiKey = "test-api-key";

        // Act
        services.AddAzureComputerVisionEmbeddings(endpoint, apiKey);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        Assert.NotNull(httpClientFactory);
    }

    [Fact]
    public void ImageEmbeddingGenerator_HasCorrectMetadata()
    {
        // Arrange
        var services = new ServiceCollection();
        var endpoint = "https://test.cognitiveservices.azure.com";
        var apiKey = "test-api-key";
        services.AddAzureComputerVisionEmbeddings(endpoint, apiKey);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var generator = serviceProvider.GetKeyedService<IEmbeddingGenerator<string, Embedding<float>>>("image");
        var metadata = generator!.GetService<EmbeddingGeneratorMetadata>();

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("AzureComputerVisionImageEmbedding", metadata.ProviderName);
    }

    [Fact]
    public void TextEmbeddingGenerator_HasCorrectMetadata()
    {
        // Arrange
        var services = new ServiceCollection();
        var endpoint = "https://test.cognitiveservices.azure.com";
        var apiKey = "test-api-key";
        services.AddAzureComputerVisionEmbeddings(endpoint, apiKey);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var generator = serviceProvider.GetKeyedService<IEmbeddingGenerator<string, Embedding<float>>>("text");
        var metadata = generator!.GetService<EmbeddingGeneratorMetadata>();

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("AzureComputerVisionTextEmbedding", metadata.ProviderName);
    }
}
