using Microsoft.Extensions.Options;

namespace MyMarketManager.WebApp.Services;

/// <summary>
/// Configuration options for the unified background processing service.
/// </summary>
public class BackgroundProcessingServiceOptions
{
    /// <summary>
    /// Gets or sets the interval between polling for batches to process.
    /// Default is 5 minutes.
    /// </summary>
    public TimeSpan BatchProcessingInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the interval between polling for images to vectorize.
    /// Default is 10 minutes.
    /// </summary>
    public TimeSpan ImageVectorizationInterval { get; set; } = TimeSpan.FromMinutes(10);
}
