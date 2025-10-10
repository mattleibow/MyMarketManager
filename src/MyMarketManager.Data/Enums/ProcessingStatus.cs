namespace MyMarketManager.Data.Enums;

/// <summary>
/// Used to track the state of purchase orders, deliveries, staging batches, and scraper sessions.
/// </summary>
public enum ProcessingStatus
{
    /// <summary>
    /// Queued to start (for scraper sessions)
    /// </summary>
    Queued = 0,

    /// <summary>
    /// Currently running or started (for scraper sessions)
    /// </summary>
    Started = 1,

    /// <summary>
    /// Fully completed
    /// </summary>
    Complete = 2,

    /// <summary>
    /// Completed successfully (for scraper sessions)
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Failed with an error
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Cancelled before completion
    /// </summary>
    Cancelled = 5
}
