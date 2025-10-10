namespace MyMarketManager.Data.Enums;

/// <summary>
/// Used to track the state of purchase orders, deliveries, staging batches, and scraper sessions.
/// </summary>
public enum ProcessingStatus
{
    /// <summary>
    /// Not yet started or awaiting action
    /// </summary>
    Pending,

    /// <summary>
    /// Partially completed or in progress
    /// </summary>
    Partial,

    /// <summary>
    /// Fully completed
    /// </summary>
    Complete,

    /// <summary>
    /// Queued to start (for scraper sessions)
    /// </summary>
    Queued,

    /// <summary>
    /// Currently running or started (for scraper sessions)
    /// </summary>
    Started,

    /// <summary>
    /// Completed successfully (for scraper sessions)
    /// </summary>
    Completed,

    /// <summary>
    /// Failed with an error
    /// </summary>
    Failed,

    /// <summary>
    /// Cancelled before completion
    /// </summary>
    Cancelled
}
