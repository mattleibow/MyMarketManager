using Microsoft.Extensions.AI;
using MyMarketManager.AI;

namespace MyMarketManager.AI.Tests;

public class NoOpEmbeddingGeneratorTests
{
    [Fact]
    public void NoOpEmbeddingGenerator_HasCorrectMetadata()
    {
        // Arrange
        var generator = new NoOpEmbeddingGenerator();

        // Act
        var metadata = generator.GetService<EmbeddingGeneratorMetadata>();

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("NoOp", metadata.ProviderName);
    }

    [Fact]
    public async Task NoOpEmbeddingGenerator_GenerateAsync_ThrowsInvalidOperationException()
    {
        // Arrange
        var generator = new NoOpEmbeddingGenerator();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => generator.GenerateAsync(["test-url"]));

        Assert.Contains("Azure AI embedding services are not configured", exception.Message);
    }

    [Fact]
    public void NoOpEmbeddingGenerator_Dispose_DoesNotThrow()
    {
        // Arrange
        var generator = new NoOpEmbeddingGenerator();

        // Act & Assert (should not throw)
        generator.Dispose();
    }
}
