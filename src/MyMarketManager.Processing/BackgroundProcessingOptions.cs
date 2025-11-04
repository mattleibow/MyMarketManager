namespace MyMarketManager.Processing;

/// <summary>
/// Configuration for the background processing service.
/// </summary>
public class BackgroundProcessingOptions
{
    /// <summary>
    /// Interval between processing cycles.
    /// Default is 5 minutes.
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMinutes(5);
}
