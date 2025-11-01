namespace MyMarketManager.WebApp.Services;

/// <summary>
/// Configuration options for the ImageVectorizationService.
/// </summary>
public class ImageVectorizationServiceOptions
{
    /// <summary>
    /// Gets or sets the interval between polling for images to vectorize.
    /// Default is 10 minutes.
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMinutes(10);
}
