namespace MyMarketManager.Data.Enums;

/// <summary>
/// Used to track the state of purchase orders, deliveries, and staging batches.
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
    Complete
}
