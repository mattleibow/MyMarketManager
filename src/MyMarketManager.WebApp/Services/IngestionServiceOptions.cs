namespace MyMarketManager.WebApp.Services;

/// <summary>
/// Configuration options for the IngestionService.
/// </summary>
public class IngestionServiceOptions
{
    /// <summary>
    /// Gets or sets the interval between polling for queued batches.
    /// Default is 5 minutes.
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMinutes(5);
}