using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;

namespace MyMarketManager.AI.Tests;

public class ExtensionsTests
{
    [Fact]
    public void AddAzureComputerVisionEmbeddings_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var endpoint = "https://test.cognitiveservices.azure.com";
        var apiKey = "test-key";

        // Act
        services.AddAzureComputerVisionEmbeddings(endpoint, apiKey);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var imageGenerator = serviceProvider.GetKeyedService<IEmbeddingGenerator<string, Embedding<float>>>("image");
        Assert.NotNull(imageGenerator);
        Assert.IsType<ImageEmbeddingGenerator>(imageGenerator);

        var textGenerator = serviceProvider.GetKeyedService<IEmbeddingGenerator<string, Embedding<float>>>("text");
        Assert.NotNull(textGenerator);
        Assert.IsType<TextEmbeddingGenerator>(textGenerator);
    }

    [Fact]
    public void AddAzureComputerVisionEmbeddings_ConfiguresHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();
        var endpoint = "https://test.cognitiveservices.azure.com";
        var apiKey = "test-key";

        // Act
        services.AddAzureComputerVisionEmbeddings(endpoint, apiKey);
        var serviceProvider = services.BuildServiceProvider();

        // Assert - verify HttpClient was registered
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        Assert.NotNull(httpClientFactory);
    }

    [Fact]
    public void AddAzureComputerVisionEmbeddings_WithCustomModelVersion_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var endpoint = "https://test.cognitiveservices.azure.com";
        var apiKey = "test-key";
        var modelVersion = "2024-01-01";

        // Act
        services.AddAzureComputerVisionEmbeddings(endpoint, apiKey, modelVersion);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var imageGenerator = serviceProvider.GetKeyedService<IEmbeddingGenerator<string, Embedding<float>>>("image");
        Assert.NotNull(imageGenerator);
    }

    [Fact]
    public void AddAzureComputerVisionEmbeddings_RegistersSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        var endpoint = "https://test.cognitiveservices.azure.com";
        var apiKey = "test-key";

        // Act
        services.AddAzureComputerVisionEmbeddings(endpoint, apiKey);
        var serviceProvider = services.BuildServiceProvider();

        // Assert - get the same instance twice
        var instance1 = serviceProvider.GetKeyedService<IEmbeddingGenerator<string, Embedding<float>>>("image");
        var instance2 = serviceProvider.GetKeyedService<IEmbeddingGenerator<string, Embedding<float>>>("image");
        Assert.Same(instance1, instance2);
    }
}
