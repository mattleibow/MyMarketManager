using Microsoft.Extensions.Logging;
using MyMarketManager.Data.Services;
using MyMarketManager.Tests.Shared;

namespace MyMarketManager.Data.Tests.Services;

public class AzureVisionServiceTests(ITestOutputHelper outputHelper)
{
    private readonly ILogger<AzureVisionService> _logger = outputHelper.ToLogger<AzureVisionService>();

    [Fact]
    public void CalculateCosineSimilarity_IdenticalVectors_ReturnsOne()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new AzureVisionService(httpClient, _logger, "https://test.com", "test-key");
        var vector1 = new float[] { 1.0f, 2.0f, 3.0f, 4.0f };
        var vector2 = new float[] { 1.0f, 2.0f, 3.0f, 4.0f };

        // Act
        var similarity = service.CalculateCosineSimilarity(vector1, vector2);

        // Assert
        Assert.Equal(1.0f, similarity, precision: 5);
    }

    [Fact]
    public void CalculateCosineSimilarity_OppositeVectors_ReturnsNegativeOne()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new AzureVisionService(httpClient, _logger, "https://test.com", "test-key");
        var vector1 = new float[] { 1.0f, 2.0f, 3.0f };
        var vector2 = new float[] { -1.0f, -2.0f, -3.0f };

        // Act
        var similarity = service.CalculateCosineSimilarity(vector1, vector2);

        // Assert
        Assert.Equal(-1.0f, similarity, precision: 5);
    }

    [Fact]
    public void CalculateCosineSimilarity_OrthogonalVectors_ReturnsZero()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new AzureVisionService(httpClient, _logger, "https://test.com", "test-key");
        var vector1 = new float[] { 1.0f, 0.0f };
        var vector2 = new float[] { 0.0f, 1.0f };

        // Act
        var similarity = service.CalculateCosineSimilarity(vector1, vector2);

        // Assert
        Assert.Equal(0.0f, similarity, precision: 5);
    }

    [Fact]
    public void CalculateCosineSimilarity_SimilarVectors_ReturnsHighValue()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new AzureVisionService(httpClient, _logger, "https://test.com", "test-key");
        var vector1 = new float[] { 1.0f, 2.0f, 3.0f };
        var vector2 = new float[] { 1.1f, 2.1f, 2.9f };

        // Act
        var similarity = service.CalculateCosineSimilarity(vector1, vector2);

        // Assert
        Assert.True(similarity > 0.99f, $"Expected similarity > 0.99, got {similarity}");
    }

    [Fact]
    public void CalculateCosineSimilarity_DifferentLengthVectors_ThrowsException()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new AzureVisionService(httpClient, _logger, "https://test.com", "test-key");
        var vector1 = new float[] { 1.0f, 2.0f };
        var vector2 = new float[] { 1.0f, 2.0f, 3.0f };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            service.CalculateCosineSimilarity(vector1, vector2));
    }

    [Fact]
    public void CalculateCosineSimilarity_ZeroVectors_ReturnsZero()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new AzureVisionService(httpClient, _logger, "https://test.com", "test-key");
        var vector1 = new float[] { 0.0f, 0.0f, 0.0f };
        var vector2 = new float[] { 1.0f, 2.0f, 3.0f };

        // Act
        var similarity = service.CalculateCosineSimilarity(vector1, vector2);

        // Assert
        Assert.Equal(0.0f, similarity);
    }
}
