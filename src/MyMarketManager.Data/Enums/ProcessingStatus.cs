namespace MyMarketManager.Data.Enums;

/// <summary>
/// Used to track the state of purchase orders, deliveries, staging batches, and scraper sessions.
/// </summary>
public enum ProcessingStatus
{
    /// <summary>
    /// Queued to start
    /// </summary>
    Queued,

    /// <summary>
    /// Currently running
    /// </summary>
    Started,

    /// <summary>
    /// Completed successfully
    /// </summary>
    Completed,

    /// <summary>
    /// Failed with an error
    /// </summary>
    Failed,

    /// <summary>
    /// Cancelled before completion
    /// </summary>
    Cancelled,
}
